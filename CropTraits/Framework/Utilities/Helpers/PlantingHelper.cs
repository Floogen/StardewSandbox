using CropTraits.Framework.Models;
using StardewValley;

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
    }
}
