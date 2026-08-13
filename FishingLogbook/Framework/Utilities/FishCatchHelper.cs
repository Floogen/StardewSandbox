using System;
using System.Collections.Generic;
using System.Linq;
using FishingLogbook.Framework.Models.Fish;
using StardewValley;

namespace FishingLogbook.Framework.Utilities
{
    /// <summary>Reads a player's fishing collection out of <see cref="Farmer.fishCaught"/>.</summary>
    public static class FishCatchHelper
    {
        /// <summary>Get a player's catch record for one fish.</summary>
        /// <param name="itemId">The qualified or unqualified item ID.</param>
        /// <param name="who">The player to read from.</param>
        public static FishCatchInfo GetCatchInfo(string itemId, Farmer who)
        {
            if (who is null || string.IsNullOrWhiteSpace(itemId))
            {
                return FishCatchInfo.None;
            }

            // The dictionary is keyed by qualified ID since 1.6, so normalise whatever the caller passed
            string qualifiedId = ItemRegistry.QualifyItemId(itemId);
            if (qualifiedId is null || !who.fishCaught.TryGetValue(qualifiedId, out int[] record))
            {
                return FishCatchInfo.None;
            }

            int timesCaught = record.Length > 0 ? record[0] : 0;
            int biggestCatch = record.Length > 1 ? record[1] : 0;
            return new FishCatchInfo(timesCaught, biggestCatch);
        }

        /// <summary>Get the combined catch record for one fish across every farmhand, for a shared display.</summary>
        /// <param name="itemId">The qualified or unqualified item ID.</param>
        public static FishCatchInfo GetCombinedCatchInfo(string itemId)
        {
            int timesCaught = 0;
            int biggestCatch = 0;

            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                FishCatchInfo info = GetCatchInfo(itemId, farmer);
                timesCaught += info.TimesCaught;
                biggestCatch = Math.Max(biggestCatch, info.BiggestCatchInches);
            }

            return new FishCatchInfo(timesCaught, biggestCatch);
        }

        /// <summary>Get the player's record catch as a fraction of the species maximum, or null if there's nothing to compare.</summary>
        /// <param name="info">The catch record.</param>
        /// <param name="fishData">The parsed <c>Data/Fish</c> entry.</param>
        public static float? GetRecordSizeRatio(FishCatchInfo info, ParsedFishData fishData)
        {
            if (!info.HasSize || fishData is null || fishData.HasSize is false)
            {
                return null;
            }

            return Math.Clamp((float)info.BiggestCatchInches / fishData.AttainableMaxSize, 0f, 1f);
        }

        /// <summary>Get how many distinct fish a player has caught, for a "42 of 78" style counter.</summary>
        /// <param name="who">The player to read from.</param>
        public static int GetDistinctCaughtCount(Farmer who)
        {
            if (who?.fishCaught is null)
            {
                return 0;
            }

            return who.fishCaught.Keys.Count(key => ItemRegistry.GetData(key) is not null);
        }

        /// <summary>Count how many of the given fish a player has caught, for a progress counter scoped to your own list.</summary>
        /// <param name="fish">The fish entries to count against.</param>
        /// <param name="who">The player to read from.</param>
        public static int GetCaughtCount(IEnumerable<FishEntry> fish, Farmer who)
        {
            if (who is null)
            {
                return 0;
            }

            return fish.Count(entry => GetCatchInfo(entry.QualifiedItemId, who).HasCaught);
        }

        /// <summary>Get the player's lifetime fish caught stat, which counts every catch rather than distinct species.</summary>
        /// <param name="who">The player to read from.</param>
        public static uint GetLifetimeFishCaught(Farmer who)
        {
            return who?.stats?.FishCaught ?? 0u;
        }

        /// <summary>Format a recorded catch size the way the game does, converting to centimetres outside English.</summary>
        /// <param name="info">The catch record.</param>
        public static string FormatBiggestCatch(FishCatchInfo info)
        {
            if (!info.HasSize)
            {
                return null;
            }
            if (LocalizedContentManager.CurrentLanguageCode is LocalizedContentManager.LanguageCode.en)
            {
                return ModEntry.translation.Get("ui.size.inches", new { size = info.BiggestCatchInches });
            }

            return ModEntry.translation.Get("ui.size.centimeters", new { size = Math.Round(info.BiggestCatchInches * 2.54) });
        }
    }
}
