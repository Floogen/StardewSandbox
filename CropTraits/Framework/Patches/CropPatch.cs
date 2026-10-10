using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CropTraits.Framework.Patches
{
    public class CropPatch : PatchTemplate
    {
        private readonly System.Type _crop = typeof(Crop);
        private static readonly MethodInfo _createItemMethod = ItemRegistryPatch.GetCreateMethod();

        // The load matching each store, so a local can be read back without Harmony's LoadLocal helper (not in every Harmony version)
        private static readonly Dictionary<OpCode, OpCode> _loadOpCodesByStoreOpCode = new()
        {
            { OpCodes.Stloc_0, OpCodes.Ldloc_0 },
            { OpCodes.Stloc_1, OpCodes.Ldloc_1 },
            { OpCodes.Stloc_2, OpCodes.Ldloc_2 },
            { OpCodes.Stloc_3, OpCodes.Ldloc_3 },
            { OpCodes.Stloc_S, OpCodes.Ldloc_S },
            { OpCodes.Stloc, OpCodes.Ldloc }
        };

        public CropPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.harvest), null), prefix: new HarmonyMethod(GetType(), nameof(HarvestPrefix)), postfix: new HarmonyMethod(GetType(), nameof(HarvestPostfix)), transpiler: new HarmonyMethod(GetType(), nameof(HarvestTranspiler)) { priority = Priority.Last }, finalizer: new HarmonyMethod(GetType(), nameof(HarvestFinalizer)));
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.newDay), null), prefix: new HarmonyMethod(GetType(), nameof(NewDayPrefix)), postfix: new HarmonyMethod(GetType(), nameof(NewDayPostfix)));
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.IsInSeason), new[] { typeof(GameLocation) }), postfix: new HarmonyMethod(GetType(), nameof(IsInSeasonPostfix)));
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.TryGrowGiantCrop), null), prefix: new HarmonyMethod(GetType(), nameof(TryGrowGiantCropPrefix)), postfix: new HarmonyMethod(GetType(), nameof(TryGrowGiantCropPostfix)));
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.draw), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(Color), typeof(float) }), prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix)));
        }

        // The grid is captured first, since the game removes the crops that form the giant crop. Traits like Stunted skip the game's attempt entirely
        private static bool TryGrowGiantCropPrefix(Crop __instance, bool checkPreconditions, out Dictionary<Vector2, TileTraitState> __state, ref bool __result)
        {
            __state = GiantCropHelper.CaptureGrid(__instance, checkPreconditions);
            if (GiantCropHelper.IsGrowthPrevented(__state))
            {
                __result = false;
                return false;
            }

            return true;
        }

        private static void TryGrowGiantCropPostfix(Crop __instance, bool checkPreconditions, Dictionary<Vector2, TileTraitState> __state, ref bool __result)
        {
            GiantCropHelper.HandleGrowthAttempt(__instance, checkPreconditions, __state, ref __result);
        }

        // Traits like Variegated tint the crop sprite (the colored overlay on flowers keeps its own color)
        private static void DrawPrefix(Crop __instance, ref Color toTint)
        {
            toTint = CropAppearanceHelper.Multiply(toTint, CropAppearanceHelper.GetCropTint(__instance));
        }

        // The state argument is changed directly, so a crop can grow as if watered without changing how its soil looks
        private static void NewDayPrefix(Crop __instance, ref int state, out CropNightState __state)
        {
            __state = CropTraits.nightlyGrowthManager.BeginCropNight(__instance, ref state);
        }

        private static void NewDayPostfix(Crop __instance, CropNightState __state)
        {
            CropTraits.nightlyGrowthManager.EndCropNight(__instance, __state);
        }

        // Covers every season check the game makes for outdoor crops (the kill in newDay, the winter removal and fertilizer clearing)
        private static void IsInSeasonPostfix(Crop __instance, GameLocation location, ref bool __result)
        {
            if (__result is false && CropTraits.nightlyGrowthManager.ExtendsSeason(__instance, location))
            {
                __result = true;
            }
        }

        private static void HarvestPrefix(Crop __instance, HoeDirt soil, JunimoHarvester junimoHarvester)
        {
            CropTraits.harvestManager.BeginHarvest(__instance, soil, junimoHarvester);
        }

        // A true result tells the caller to remove the crop, so traits like Resprouting flip it to keep the crop planted while traits like Spent flip it to remove a regrowing crop
        private static void HarvestPostfix(Crop __instance, int xTile, int yTile, HoeDirt soil, ref bool __result)
        {
            CropTraits.harvestManager.CompleteHarvest(xTile, yTile, soil?.Location ?? Game1.currentLocation);
            CropTraits.harvestManager.TryRegrow(__instance, soil, ref __result);
            CropTraits.harvestManager.TryRemoveSpentCrop(__instance, soil, ref __result);
            CropTraits.harvestManager.CompleteCropRemoval(__instance, soil, __result);
        }

        /// <summary>
        /// Adjusts the quality right before the harvest item is created, so the change carries into every copy given to the player or a Junimo.
        /// The quality local is found by what it's used for (the quality passed to ItemRegistry.Create) rather than by how the game picks it.
        /// Running last with the adjustment after the final store means other mods that change the quality are applied first, then traits adjust their result.
        /// </summary>
        private static IEnumerable<CodeInstruction> HarvestTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            // Matches "ItemRegistry.Create(indexOfHarvest.Value, 1, cropQuality)", where the quality is loaded just before the default allowNull argument
            int qualityLoadIndex = -1;
            for (int index = 2; index < codes.Count; index++)
            {
                if (codes[index].Calls(_createItemMethod) && codes[index - 2].IsLdloc())
                {
                    qualityLoadIndex = index - 2;
                    break;
                }
            }

            int qualityStoreIndex = qualityLoadIndex < 0 ? -1 : codes.FindLastIndex(qualityLoadIndex, code => IsStoreToLocal(code, codes[qualityLoadIndex]));
            if (qualityStoreIndex < 0)
            {
                _monitor.Log("Couldn't find the harvest quality in Crop.harvest, so traits like Erratic won't change harvest quality.", LogLevel.Warn);
                return codes;
            }

            // The quality is read and written back through the same local the game stores it in
            CodeInstruction qualityStore = codes[qualityStoreIndex];
            CodeInstruction loadQuality = new CodeInstruction(codes[qualityLoadIndex].opcode, codes[qualityLoadIndex].operand);
            CodeInstruction storeQuality = new CodeInstruction(qualityStore.opcode, qualityStore.operand);

            // Labels move to the new first instruction, so jumps into this spot still run the adjustment
            loadQuality.WithLabels(codes[qualityStoreIndex + 1].ExtractLabels());
            codes.InsertRange(qualityStoreIndex + 1, new[] { loadQuality, new CodeInstruction(OpCodes.Ldarg_0), CodeInstruction.Call(typeof(CropPatch), nameof(ModifyHarvestQuality)), storeQuality });

            return codes;
        }

        public static bool IsStoreToLocal(CodeInstruction store, CodeInstruction load)
        {
            return _loadOpCodesByStoreOpCode.TryGetValue(store.opcode, out OpCode loadOpCode) && loadOpCode == load.opcode && Equals(store.operand, load.operand);
        }

        public static int ModifyHarvestQuality(int quality, Crop crop)
        {
            return CropTraits.harvestManager.ModifyHarvestQuality(quality, crop);
        }

        // Finalizer ensures the harvest context clears even if harvest throws
        private static void HarvestFinalizer()
        {
            CropTraits.harvestManager.EndHarvest();
        }
    }
}
