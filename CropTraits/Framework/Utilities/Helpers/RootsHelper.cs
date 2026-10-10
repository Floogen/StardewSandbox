using CropTraits.Framework.Models;
using StardewValley;

namespace CropTraits.Framework.Utilities.Helpers
{
    // Shared by the Roots traits, which drop ore or geodes on top of the normal harvest from crops that don't grow back
    public static class RootsHelper
    {
        public const string COPPER_ORE_ID = "(O)378";
        public const string IRON_ORE_ID = "(O)380";
        public const string GOLD_ORE_ID = "(O)384";
        public const string IRIDIUM_ORE_ID = "(O)386";
        public const string GEODE_ID = "(O)535";
        public const string FROZEN_GEODE_ID = "(O)536";
        public const string OMNI_GEODE_ID = "(O)749";

        public const int MIN_ORE_COUNT = 1;

        // Ore counts lean toward the top of the range on better quality harvests
        public static void DropOre(HarvestContext context, string oreId, int maxCount, int xTile, int yTile, GameLocation location)
        {
            if (HarvestDropHelper.IsNonRenewable(context) is false || Game1.random.NextDouble() >= HarvestDropHelper.GetHarvestPaceMultiplier(context))
            {
                return;
            }

            int count = HarvestDropHelper.RollBestOf(MIN_ORE_COUNT, maxCount, HarvestDropHelper.GetHarvestQuality(context), Game1.random);
            HarvestDropHelper.DropNew(context, oreId, count, xTile, yTile, location);
        }

        // Geodes drop one at most, whatever the harvest quality
        public static void DropGeode(HarvestContext context, string geodeId, int xTile, int yTile, GameLocation location)
        {
            if (HarvestDropHelper.IsNonRenewable(context) is false || Game1.random.NextDouble() >= HarvestDropHelper.GetHarvestPaceMultiplier(context))
            {
                return;
            }

            HarvestDropHelper.DropNew(context, geodeId, 1, xTile, yTile, location);
        }

        public static string GetOreRangeText(int maxCount, string oreName)
        {
            return maxCount <= MIN_ORE_COUNT ? $"Drops {MIN_ORE_COUNT} {oreName} when harvested" : $"Drops {MIN_ORE_COUNT} to {maxCount} {oreName} when harvested";
        }

        public static string GetOreNotes()
        {
            return $"Dropped on top of the normal harvest. Better quality harvests lean toward the top of the range. {HarvestDropHelper.GetHarvestPaceNote()} Only appears on crops that don't grow back. Can't share a crop with Resprouting or other Roots traits.";
        }

        public static string GetGeodeNotes()
        {
            return $"Dropped on top of the normal harvest, one geode whatever the quality. {HarvestDropHelper.GetHarvestPaceNote()} Only appears on crops that don't grow back. Can't share a crop with Resprouting or other Roots traits.";
        }
    }
}
