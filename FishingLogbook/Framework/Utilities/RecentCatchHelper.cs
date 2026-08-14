using FishingLogbook.Framework.Models.Fish;
using Newtonsoft.Json;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FishingLogbook.Framework.Utilities
{
    /// <summary>Handles tracking the most recent fishing caches.</summary>
    public static class RecentCatchHelper
    {
        public const string RECENT_CATCHES_MOD_DATA_KEY = "Mods/PeacefulEnd.FishingLogbook/RecentCatches";

        /// <summary>Get a player's recent catch records.</summary>
        /// <param name="who">The player to read from.</param>
        public static List<RecentCatchInfo> GetRecentCatches(Farmer who)
        {
            if (who is null || who.modData.TryGetValue(RECENT_CATCHES_MOD_DATA_KEY, out var recentCatchesText) is false)
            {
                return new List<RecentCatchInfo>();
            }

            return JsonConvert.DeserializeObject<List<RecentCatchInfo>>(recentCatchesText) ?? new List<RecentCatchInfo>();
        }

        /// <summary>Get a player's recent catch records, skipping any whose item no longer resolves.</summary>
        /// <param name="who">The player to read from.</param>
        public static List<RecentCatchInfo> GetResolvableRecentCatches(Farmer who)
        {
            return GetRecentCatches(who).Where(entry => string.IsNullOrWhiteSpace(entry?.Id) is false && ItemRegistry.GetData(entry.Id) is not null).ToList();
        }

        /// <summary>
        /// Add to the player's recent catch records.
        /// </summary>
        /// <param name="who">The player to write the cache to.</param>
        /// <param name="fishId">The qualified or unqualified item ID.</param>
        /// <param name="size">The fish's size in inches.</param>
        /// <param name="limitEntries">Limits the amount of entries that should be cached.</param>
        public static void AddRecentCatch(Farmer who, string fishId, int size, int limitEntries = 10)
        {
            var recentCatches = GetRecentCatches(who);

            string qualifiedId = ItemRegistry.QualifyItemId(fishId);
            if (qualifiedId is null)
            {
                return;
            }
            recentCatches.Add(new RecentCatchInfo() { Id = qualifiedId, SizeInInches = size });

            // Cache the data
            who.modData[RECENT_CATCHES_MOD_DATA_KEY] = JsonConvert.SerializeObject(recentCatches.TakeLast(limitEntries));
        }
    }
}
