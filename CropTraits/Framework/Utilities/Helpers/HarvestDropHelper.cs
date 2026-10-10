using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Crops;
using System;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class HarvestDropHelper
    {
        public const int FULL_CHANCE_DAYS_PER_HARVEST = 8;

        // Junimos carry bonus items to their hut like the main harvest, otherwise they drop on the crop's tile
        public static void Drop(HarvestContext context, Item item, int xTile, int yTile, GameLocation location)
        {
            if (item is null)
            {
                return;
            }

            if (context.JunimoHarvester is not null)
            {
                context.JunimoHarvester.tryToAddItemToHut(item);
                return;
            }

            Game1.createItemDebris(item, new Vector2((xTile * Game1.tileSize) + (Game1.tileSize / 2), (yTile * Game1.tileSize) + (Game1.tileSize / 2)), -1, location);
        }

        // Bonus items are created inside the harvest context, but only items matching the harvest pick up traits, so these stay plain
        public static void DropNew(HarvestContext context, string qualifiedItemId, int count, int xTile, int yTile, GameLocation location)
        {
            if (count <= 0 || string.IsNullOrEmpty(qualifiedItemId))
            {
                return;
            }

            Drop(context, ItemRegistry.Create(qualifiedItemId, count), xTile, yTile, location);
        }

        // Crops that grow back after harvest are renewable, so traits limited to single harvest crops skip them
        public static bool IsNonRenewable(CropData cropData)
        {
            return cropData is not null && cropData.RegrowDays <= 0;
        }

        // The crop's own data, since the harvest context only knows the seed it grew from
        public static bool IsNonRenewable(HarvestContext context)
        {
            return string.IsNullOrEmpty(context.SeedId) is false && Crop.TryGetData(context.SeedId, out CropData cropData) && IsNonRenewable(cropData);
        }

        /// <summary>
        /// Scales per-harvest bonus drops by how long the crop takes to produce, so a crop that takes fewer than eight days per harvest earns the same bonus per day as an eight day crop.
        /// Uses the crop's own data before any speed modifiers (Speed-Gro, Agriculturist or traits like Swift). Crops that regrow use their regrow days, since most of their harvests are regrowths.
        /// </summary>
        public static double GetHarvestPaceMultiplier(HarvestContext context)
        {
            if (string.IsNullOrEmpty(context.SeedId) || Crop.TryGetData(context.SeedId, out CropData cropData) is false)
            {
                return 1;
            }

            int daysPerHarvest = cropData.RegrowDays > 0 ? cropData.RegrowDays : GrowthHelper.GetBaseGrowthDays(cropData);
            if (daysPerHarvest <= 0)
            {
                return 1;
            }

            return Math.Min(1.0, (double)daysPerHarvest / FULL_CHANCE_DAYS_PER_HARVEST);
        }

        public static string GetHarvestPaceNote()
        {
            return $"Crops that take fewer than {FULL_CHANCE_DAYS_PER_HARVEST} days per harvest (before speed boosts) have a lower chance, so faster crops don't earn more per day.";
        }

        public static int GetHarvestQuality(HarvestContext context)
        {
            return context.HarvestedItem?.Quality ?? StardewValley.Object.lowQuality;
        }

        // Takes the best of one roll per quality step (one at normal up to four at iridium), so better harvests lean toward the top of the range
        public static int RollBestOf(int minCount, int maxCount, int quality, Random random)
        {
            int rolls = 1 + QualityHelper.GetQualityStep(quality);
            int bestCount = minCount;
            for (int roll = 0; roll < rolls; roll++)
            {
                bestCount = Math.Max(bestCount, random.Next(minCount, maxCount + 1));
            }

            return bestCount;
        }
    }
}
