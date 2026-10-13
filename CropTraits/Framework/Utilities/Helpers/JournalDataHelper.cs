using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using StardewValley;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    // Gathers what Field Notes shows from the crop data and the discovery data
    public static class JournalDataHelper
    {
        public static List<JournalCropEntry> GetCrops()
        {
            List<JournalCropEntry> crops = new();
            foreach (KeyValuePair<string, CropData> pair in Game1.cropData)
            {
                if (string.IsNullOrEmpty(pair.Value?.HarvestItemId) || ItemRegistry.Exists(ItemRegistry.QualifyItemId(pair.Key)) is false)
                {
                    continue;
                }

                string harvestItemId = ItemRegistry.QualifyItemId(pair.Value.HarvestItemId);
                if (string.IsNullOrEmpty(harvestItemId) || ItemRegistry.Exists(harvestItemId) is false)
                {
                    continue;
                }

                crops.Add(new JournalCropEntry(pair.Key, harvestItemId, ItemRegistry.GetData(harvestItemId).DisplayName));
            }

            return crops
                .OrderBy(crop => crop.DisplayName, StringComparer.CurrentCulture)
                .ThenBy(crop => crop.SeedId, StringComparer.Ordinal)
                .ToList();
        }

        public static bool IsDiscovered(string seedId)
        {
            return CropTraits.discoveryManager.Data.Crops.TryGetValue(seedId, out CropDiscoveryData crop) && crop.TraitTiers.Count > 0;
        }

        public static CropDiscoveryData GetCropRecord(string seedId)
        {
            return CropTraits.discoveryManager.Data.Crops.TryGetValue(seedId, out CropDiscoveryData crop) ? crop : new CropDiscoveryData();
        }

        // Newest first
        public static List<BlendJournalEntryData> GetCrossbreeds(string seedId)
        {
            return CropTraits.discoveryManager.Data.BlendJournal.Values
                .Where(entry => entry.SeedId == seedId)
                .OrderByDescending(entry => entry.FirstSplicedDay)
                .ToList();
        }

        // The harvest item's name, falling back to the seed's own name
        public static string GetCropName(string seedId)
        {
            if (string.IsNullOrEmpty(seedId))
            {
                return string.Empty;
            }

            if (Game1.cropData.TryGetValue(seedId, out CropData cropData) && string.IsNullOrEmpty(cropData.HarvestItemId) is false)
            {
                return ItemRegistry.GetDataOrErrorItem(cropData.HarvestItemId).DisplayName;
            }

            return ItemRegistry.GetDataOrErrorItem(seedId).DisplayName;
        }

        public static string GetQualityName(int quality)
        {
            string qualityKey = quality switch
            {
                StardewValley.Object.medQuality => "silver",
                StardewValley.Object.highQuality => "gold",
                StardewValley.Object.bestQuality => "iridium",
                _ => "normal"
            };

            return TranslationHelper.Get($"journal.quality.{qualityKey}");
        }
    }
}
