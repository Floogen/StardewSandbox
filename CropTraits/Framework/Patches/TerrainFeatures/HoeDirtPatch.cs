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
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.plant), new[] { typeof(string), typeof(Farmer), typeof(bool) }), postfix: new HarmonyMethod(GetType(), nameof(PlantPostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.GetFertilizerQualityBoostLevel), null), postfix: new HarmonyMethod(GetType(), nameof(GetFertilizerQualityBoostLevelPostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.GetFertilizerSpeedBoost), null), postfix: new HarmonyMethod(GetType(), nameof(GetFertilizerSpeedBoostPostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.GetFertilizerWaterRetentionChance), null), postfix: new HarmonyMethod(GetType(), nameof(GetFertilizerWaterRetentionChancePostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.applySpeedIncreases), null), postfix: new HarmonyMethod(GetType(), nameof(ApplySpeedIncreasesPostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.GetFertilizerSourceRect), null), postfix: new HarmonyMethod(GetType(), nameof(GetFertilizerSourceRectPostfix)));
            harmony.Patch(AccessTools.Method(_hoeDirt, nameof(HoeDirt.DrawOptimized), null), transpiler: new HarmonyMethod(GetType(), nameof(DrawOptimizedTranspiler)));
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
            __instance.modData.Remove(CropTraits.TRAITS_MOD_DATA_KEY);
            __instance.modData.Remove(CropTraits.SEED_TRAITS_MOD_DATA_KEY);
            __instance.modData.Remove(CropTraits.PLANTER_ID_MOD_DATA_KEY);
            __instance.modData.Remove(MutationManager.HAS_MUTATED_MOD_DATA_KEY);
            __instance.modData.Remove(NightlyGrowthManager.OUT_OF_SEASON_NIGHTS_MOD_DATA_KEY);

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
