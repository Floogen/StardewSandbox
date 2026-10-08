using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class CropDataHelper
    {
        public static ParsedItemData GetHarvestData(CropData cropData)
        {
            if (string.IsNullOrEmpty(cropData?.HarvestItemId))
            {
                return null;
            }

            return ItemRegistry.GetData(cropData.HarvestItemId);
        }

        // Produce players can eat for energy, which leaves out items like Coffee Beans and Fiber
        public static bool HasEdibleHarvest(CropData cropData)
        {
            return GetHarvestData(cropData)?.RawData is ObjectData objectData && objectData.Edibility > 0;
        }

        public static bool HasHarvestCategory(CropData cropData, params int[] categories)
        {
            ParsedItemData harvestData = GetHarvestData(cropData);
            return harvestData is not null && categories.Contains(harvestData.Category);
        }
    }
}
