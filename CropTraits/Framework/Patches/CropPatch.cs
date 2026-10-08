using CropTraits.Framework.Models;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CropTraits.Framework.Patches
{
    public class CropPatch : PatchTemplate
    {
        private readonly System.Type _crop = typeof(Crop);
        private static readonly MethodInfo _clampMethod = AccessTools.Method(typeof(MathHelper), nameof(MathHelper.Clamp), new[] { typeof(int), typeof(int), typeof(int) });

        public CropPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.harvest), null), prefix: new HarmonyMethod(GetType(), nameof(HarvestPrefix)), postfix: new HarmonyMethod(GetType(), nameof(HarvestPostfix)), transpiler: new HarmonyMethod(GetType(), nameof(HarvestTranspiler)), finalizer: new HarmonyMethod(GetType(), nameof(HarvestFinalizer)));
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.newDay), null), prefix: new HarmonyMethod(GetType(), nameof(NewDayPrefix)), postfix: new HarmonyMethod(GetType(), nameof(NewDayPostfix)));
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.IsInSeason), new[] { typeof(GameLocation) }), postfix: new HarmonyMethod(GetType(), nameof(IsInSeasonPostfix)));
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

        // A true result tells the caller to remove the crop, so traits like Resprouting flip it to keep the crop planted
        private static void HarvestPostfix(Crop __instance, int xTile, int yTile, HoeDirt soil, ref bool __result)
        {
            CropTraits.harvestManager.CompleteHarvest(xTile, yTile, soil?.Location ?? Game1.currentLocation);
            CropTraits.harvestManager.TryRegrow(__instance, soil, ref __result);
        }

        // Adjusts the quality right after the game picks and clamps it, before the harvest is copied for the player or a Junimo
        private static IEnumerable<CodeInstruction> HarvestTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions).MatchEndForward(new CodeMatch(instruction => instruction.Calls(_clampMethod)), new CodeMatch(instruction => instruction.IsStloc()));
            if (matcher.IsInvalid || _loadOpCodesByStoreOpCode.TryGetValue(matcher.Opcode, out OpCode loadOpCode) is false)
            {
                _monitor.Log("Couldn't find the harvest quality in Crop.harvest, so traits like Erratic won't change harvest quality.", LogLevel.Warn);
                return instructions;
            }

            // The quality is read and written back through the same local the game stores it in
            CodeInstruction qualityStore = matcher.Instruction;
            CodeInstruction loadQuality = new CodeInstruction(loadOpCode, qualityStore.operand);
            CodeInstruction storeQuality = new CodeInstruction(qualityStore.opcode, qualityStore.operand);

            // Labels move to the new first instruction, so jumps into this spot still run the adjustment
            CodeInstruction nextInstruction = matcher.Advance(1).Instruction;
            matcher.Insert(loadQuality.WithLabels(nextInstruction.ExtractLabels()), new CodeInstruction(OpCodes.Ldarg_0), CodeInstruction.Call(typeof(CropPatch), nameof(ModifyHarvestQuality)), storeQuality);

            return matcher.InstructionEnumeration();
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
