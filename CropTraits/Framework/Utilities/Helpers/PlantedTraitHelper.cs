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

        public static TraitSet GetVisibleTraits(HoeDirt dirt)
        {
            if (dirt?.crop is null || dirt.crop.dead.Value)
            {
                return new TraitSet();
            }

            return IsMature(dirt.crop) ? TraitModDataHelper.GetTraits(dirt.modData) : GetSeedTraits(dirt);
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
