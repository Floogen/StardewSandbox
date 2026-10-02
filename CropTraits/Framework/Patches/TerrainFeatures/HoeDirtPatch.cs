using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

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

            GrowthHelper.ApplyGrowthTraits(__instance.crop, TraitSet.FromKey(traitKey));
        }

        // Quality traits feed into vanilla's harvest quality odds, so they work the same for every crop and for Junimos
        private static void GetFertilizerQualityBoostLevelPostfix(HoeDirt __instance, ref int __result)
        {
            TraitSet traits = TraitModDataHelper.GetTraits(__instance.modData);
            if (traits.Count == 0)
            {
                return;
            }

            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(traits))
            {
                __result = trait.ModifyQualityBoostLevel(__result, tier);
            }
        }
    }
}
