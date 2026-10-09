using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace CropTraits.Framework.Patches.TerrainFeatures
{
    public class HoeDirtPatch : PatchTemplate
    {
        private readonly System.Type _hoeDirt = typeof(HoeDirt);

        public HoeDirtPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.plant), new[] { typeof(string), typeof(Farmer), typeof(bool) }), prefix: new HarmonyMethod(GetType(), nameof(PlantPrefix)), postfix: new HarmonyMethod(GetType(), nameof(PlantPostfix)), finalizer: new HarmonyMethod(GetType(), nameof(EndSeasonCheckFinalizer)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.canPlantThisSeedHere), null), prefix: new HarmonyMethod(GetType(), nameof(CanPlantThisSeedHerePrefix)), finalizer: new HarmonyMethod(GetType(), nameof(EndSeasonCheckFinalizer)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.GetFertilizerQualityBoostLevel), null), postfix: new HarmonyMethod(GetType(), nameof(GetFertilizerQualityBoostLevelPostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.GetFertilizerSpeedBoost), null), postfix: new HarmonyMethod(GetType(), nameof(GetFertilizerSpeedBoostPostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.GetFertilizerWaterRetentionChance), null), postfix: new HarmonyMethod(GetType(), nameof(GetFertilizerWaterRetentionChancePostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.applySpeedIncreases), null), postfix: new HarmonyMethod(GetType(), nameof(ApplySpeedIncreasesPostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.GetFertilizerSourceRect), null), postfix: new HarmonyMethod(GetType(), nameof(GetFertilizerSourceRectPostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.DrawOptimized), null), transpiler: new HarmonyMethod(GetType(), nameof(DrawOptimizedTranspiler)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.performToolAction), null), prefix: new HarmonyMethod(GetType(), nameof(PerformToolActionPrefix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.isPassable), null), postfix: new HarmonyMethod(GetType(), nameof(IsPassablePostfix)));
        }

        // Traits like Trailing let players walk through trellis crops, while traits like Climbing make other crops block the way
        // The character is read by position (__0), so a renamed game parameter can't break the patch
        private static void IsPassablePostfix(TerrainFeature __instance, Character __0, ref bool __result)
        {
            if (__instance is HoeDirt dirt && dirt.crop is not null)
            {
                __result = PassabilityHelper.IsPassable(dirt, __0, __result);
            }
        }

        // The game's lightning hits soil with no tool, so traits like Conductive turn that hit into full growth instead of a dead crop
        private static bool PerformToolActionPrefix(HoeDirt __instance, Tool t, ref bool __result)
        {
            if (t is not null || LightningHelper.IsLightningUpdateActive is false || LightningHelper.IsStrikeTarget(__instance, includeReady: true) is false)
            {
                return true;
            }

            LightningHelper.Strike(Game1.getFarm(), __instance.Tile, __instance);
            __result = false;
            return false;
        }

        private static void GetFertilizerSourceRectPostfix(HoeDirt __instance, ref Rectangle __result)
        {
            if (FertilizerHelper.IsTextureSwapActive && MutationHelper.HasMutagenicFertilizer(__instance))
            {
                __result = AssetManager.MUTAGENIC_FERTILIZER_DIRT_SOURCE;
            }
        }

        private static IEnumerable<CodeInstruction> DrawOptimizedTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return FertilizerHelper.ReplaceFertilizerTexture(instructions, _monitor, $"{nameof(HoeDirt)}.{nameof(HoeDirt.DrawOptimized)}", true);
        }

        // Seeds that grow in every season skip the season check (the held seed carries the traits)
        private static void PlantPrefix(string itemId, Farmer who, bool isFertilizer)
        {
            if (isFertilizer is false)
            {
                PlantingHelper.BeginSeasonCheck(who?.ActiveObject, itemId);
            }
        }

        // Used for the placement highlight, which only ever checks the local player's held seed
        private static void CanPlantThisSeedHerePrefix(string itemId, bool isFertilizer)
        {
            if (isFertilizer is false)
            {
                PlantingHelper.BeginSeasonCheck(Game1.player?.ActiveObject, itemId);
            }
        }

        // A finalizer so the season check always ends, even if the game throws
        private static void EndSeasonCheckFinalizer()
        {
            PlantingHelper.EndSeasonCheck();
        }

        private static void PlantPostfix(HoeDirt __instance, string itemId, Farmer who, bool isFertilizer, bool __result)
        {
            if (__result is false || __instance.crop is null)
            {
                return;
            }

            if (isFertilizer)
            {
                // Vanilla resets the phase lengths when fertilizer is applied, which would wipe growth traits
                TraitSet traits = TraitModDataHelper.GetTraits(__instance.modData);
                if (traits.Count > 0)
                {
                    GrowthHelper.ReapplyGrowthTraits(__instance, traits, GrowthHelper.GetPlanter(__instance, who));
                }

                return;
            }

            // HoeDirt outlives its crop, so leftover state from a previous crop is cleared
            PlantingHelper.ResetCropModData(__instance);

            if (who is not null)
            {
                __instance.modData[CropTraits.PLANTER_ID_MOD_DATA_KEY] = who.UniqueMultiplayerID.ToString();
            }

            __instance.modData[MutationManager.PENDING_MUTATION_MOD_DATA_KEY] = "true";

            // Planters that don't hold the seed (other mods) plant untraited crops
            Item heldItem = who?.ActiveObject;
            if (heldItem is null || heldItem.QualifiedItemId != ItemRegistry.QualifyItemId(itemId))
            {
                return;
            }

            string traitKey = TraitModDataHelper.GetTraitKey(heldItem.modData);
            if (string.IsNullOrEmpty(traitKey))
            {
                return;
            }

            __instance.modData[CropTraits.TRAITS_MOD_DATA_KEY] = traitKey;
            __instance.modData[CropTraits.SEED_TRAITS_MOD_DATA_KEY] = traitKey;

            // Vanilla applied its speed boosts before the traits were set, so the phases are rebuilt for traits like Fussy and Balanced that ignore them
            GrowthHelper.ReapplyGrowthTraits(__instance, TraitSet.FromKey(traitKey), who);
        }

        // Quality traits feed into vanilla's harvest quality odds, so they work the same for every crop and for Junimos
        private static void GetFertilizerQualityBoostLevelPostfix(HoeDirt __instance, ref int __result)
        {
            TraitSet traits = TraitModDataHelper.GetTraits(__instance.modData);
            if (traits.Count == 0)
            {
                return;
            }

            // Traits like Fussy drop the fertilizer's levels first, so quality traits like Regal still add theirs
            if (CropTraits.traitManager.IgnoresFertilizer(traits))
            {
                __result = 0;
            }

            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(traits))
            {
                __result = trait.ModifyQualityBoostLevel(__result, tier);
            }
        }

        private static void GetFertilizerSpeedBoostPostfix(HoeDirt __instance, ref float __result)
        {
            if (__result != 0 && CropTraits.traitManager.IgnoresFertilizer(TraitModDataHelper.GetTraits(__instance.modData)))
            {
                __result = 0;
            }
        }

        private static void GetFertilizerWaterRetentionChancePostfix(HoeDirt __instance, ref float __result)
        {
            if (__result != 0 && CropTraits.traitManager.IgnoresFertilizer(TraitModDataHelper.GetTraits(__instance.modData)))
            {
                __result = 0;
            }
        }

        // Traits like Balanced ignore Speed-Gro, Agriculturist and the paddy bonus, so the phases go back to the crop's data
        private static void ApplySpeedIncreasesPostfix(HoeDirt __instance)
        {
            if (__instance.crop is not null && CropTraits.traitManager.IgnoresGrowthModifiers(TraitModDataHelper.GetTraits(__instance.modData)))
            {
                __instance.crop.ResetPhaseDays();
            }
        }
    }
}
