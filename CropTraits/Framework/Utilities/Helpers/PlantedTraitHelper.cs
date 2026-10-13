using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class PlantedTraitHelper
    {
        public static bool IsMature(Crop crop)
        {
            if (crop is null)
            {
                return false;
            }

            return crop.fullyGrown.Value || crop.currentPhase.Value >= crop.phaseDays.Count - 1;
        }

        // Before maturity only the seed's traits are visible, so a hidden mutation doesn't show early. The set is cached and shared, so it must never be changed
        public static TraitSet GetVisibleTraits(HoeDirt dirt)
        {
            if (dirt?.crop is null || dirt.crop.dead.Value)
            {
                return TraitModDataHelper.GetCachedTraits(string.Empty);
            }

            string modDataKey = IsMature(dirt.crop) ? CropTraits.TRAITS_MOD_DATA_KEY : CropTraits.SEED_TRAITS_MOD_DATA_KEY;
            return TraitModDataHelper.GetCachedTraits(dirt.modData.TryGetValue(modDataKey, out string traitKey) ? traitKey : string.Empty);
        }

        public static TraitSet GetSeedTraits(HoeDirt dirt)
        {
            return dirt.modData.TryGetValue(CropTraits.SEED_TRAITS_MOD_DATA_KEY, out string seedTraitKey) ? TraitSet.FromKey(seedTraitKey) : new TraitSet();
        }

        public static HashSet<string> GetNewTraitIds(HoeDirt dirt)
        {
            HashSet<string> newTraitIds = new();
            if (dirt?.crop is null || IsMature(dirt.crop) is false)
            {
                return newTraitIds;
            }

            TraitSet seedTraits = GetSeedTraits(dirt);
            foreach (KeyValuePair<string, int> trait in TraitModDataHelper.GetTraits(dirt.modData).Entries)
            {
                if (trait.Value > seedTraits.GetTier(trait.Key))
                {
                    newTraitIds.Add(trait.Key);
                }
            }

            return newTraitIds;
        }

        public static bool IsUnharvestedMutation(HoeDirt dirt)
        {
            if (dirt?.crop is null || dirt.crop.dead.Value || dirt.crop.fullyGrown.Value is false)
            {
                return false;
            }

            return GetNewTraitIds(dirt).Count > 0;
        }

        public static bool TryGetDirt(GameLocation location, Vector2 tile, out HoeDirt dirt)
        {
            if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) && feature is HoeDirt groundDirt)
            {
                dirt = groundDirt;
                return true;
            }

            if (location.objects.TryGetValue(tile, out Object placedObject) && placedObject is IndoorPot pot && pot.hoeDirt.Value is not null)
            {
                dirt = pot.hoeDirt.Value;
                return true;
            }

            dirt = null;
            return false;
        }

        public static string GetCropName(Crop crop)
        {
            string itemId = string.IsNullOrEmpty(crop.indexOfHarvest.Value) ? crop.netSeedIndex.Value : crop.indexOfHarvest.Value;
            return ItemRegistry.GetDataOrErrorItem(itemId).DisplayName;
        }
    }
}
