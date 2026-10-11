using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class PlantingHelper
    {
        // Set only while the game checks or plants a seed that grows in every season, so the location skips its season check like the greenhouse does
        public static bool IsPlantingSeasonlessSeed { get; private set; }

        public static void BeginSeasonCheck(Item seed, string seedId)
        {
            IsPlantingSeasonlessSeed = false;
            if (seed is null || string.IsNullOrEmpty(seedId) || seed.ItemId != seedId)
            {
                return;
            }

            TraitSet traits = TraitModDataHelper.GetTraits(seed.modData);
            IsPlantingSeasonlessSeed = traits.Count > 0 && CropTraits.traitManager.GrowsInEverySeason(traits);
        }

        public static void EndSeasonCheck()
        {
            IsPlantingSeasonlessSeed = false;
        }

        // HoeDirt outlives its crop, so leftover state from a previous crop is cleared before a new one starts
        public static void ResetCropModData(HoeDirt dirt)
        {
            dirt.modData.Remove(CropTraits.TRAITS_MOD_DATA_KEY);
            dirt.modData.Remove(CropTraits.SEED_TRAITS_MOD_DATA_KEY);
            dirt.modData.Remove(CropTraits.PLANTER_ID_MOD_DATA_KEY);
            dirt.modData.Remove(MutationManager.PENDING_MUTATION_MOD_DATA_KEY);
            dirt.modData.Remove(MutationManager.HAS_MUTATED_MOD_DATA_KEY);
            dirt.modData.Remove(NightlyGrowthManager.OUT_OF_SEASON_NIGHTS_MOD_DATA_KEY);
            dirt.modData.Remove(NightlyGrowthManager.DAYS_LEFT_READY_MOD_DATA_KEY);
            dirt.modData.Remove(HarvestManager.REGROWTHS_USED_MOD_DATA_KEY);
            dirt.modData.Remove(SelfSeedingTrait.HAS_SPREAD_MOD_DATA_KEY);
            dirt.modData.Remove(ContagiousTrait.HAS_SPREAD_MOD_DATA_KEY);
            dirt.modData.Remove(MimicTrait.HAS_COPIED_MOD_DATA_KEY);
        }

        /// <summary>
        /// Plants a seedling of the crop in a random empty tilled tile around it, carrying the same traits. Traits like Eager start it halfway grown.
        /// The seedling skips its first-night mutation roll, since it starts as a copy rather than a new planting. Returns false when there's no room.
        /// </summary>
        public static bool TrySpreadCrop(GameLocation location, Vector2 tile, HoeDirt parentDirt, Random random, bool startsHalfwayGrown)
        {
            Crop parentCrop = parentDirt?.crop;
            if (location is null || parentCrop is null || parentCrop.dead.Value || parentCrop.forageCrop.Value)
            {
                return false;
            }

            // Only ground soil counts, so garden pots and tiles with something placed on them are left alone
            List<(Vector2 Tile, HoeDirt Dirt)> emptyDirt = new();
            foreach (Vector2 neighborTile in Utility.getSurroundingTileLocationsArray(tile))
            {
                if (location.terrainFeatures.TryGetValue(neighborTile, out TerrainFeature feature) && feature is HoeDirt dirt && dirt.crop is null && location.objects.ContainsKey(neighborTile) is false)
                {
                    emptyDirt.Add((neighborTile, dirt));
                }
            }

            if (emptyDirt.Count == 0)
            {
                return false;
            }

            (Vector2 targetTile, HoeDirt targetDirt) = emptyDirt[random.Next(emptyDirt.Count)];
            string traitKey = TraitModDataHelper.GetTraitKey(parentDirt.modData);

            ResetCropModData(targetDirt);
            if (parentDirt.modData.TryGetValue(CropTraits.PLANTER_ID_MOD_DATA_KEY, out string planterId))
            {
                targetDirt.modData[CropTraits.PLANTER_ID_MOD_DATA_KEY] = planterId;
            }

            // The seedling's traits are already known, so they show right away instead of waiting for maturity
            if (string.IsNullOrEmpty(traitKey) is false)
            {
                targetDirt.modData[CropTraits.TRAITS_MOD_DATA_KEY] = traitKey;
                targetDirt.modData[CropTraits.SEED_TRAITS_MOD_DATA_KEY] = traitKey;
            }

            // Matches the setup in HoeDirt.plant, without its sounds or the seeds sown stat
            targetDirt.crop = new Crop(parentCrop.netSeedIndex.Value, (int)targetTile.X, (int)targetTile.Y, location);
            targetDirt.nearWaterForPaddy.Value = -1;

            GrowthHelper.ReapplyGrowthTraits(targetDirt, TraitSet.FromKey(traitKey), GrowthHelper.GetPlanter(targetDirt, Game1.MasterPlayer));
            if (startsHalfwayGrown)
            {
                GrowthHelper.ResetToHalfwayGrowth(targetDirt.crop);
            }

            return true;
        }
    }
}
