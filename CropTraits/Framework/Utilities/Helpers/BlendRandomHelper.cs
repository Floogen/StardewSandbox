using CropTraits.Framework.Models;
using StardewValley;
using System;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class BlendRandomHelper
    {
        // Canonical order makes results independent of which slot each parent is in
        public static (BlendParent First, BlendParent Second) GetCanonicalOrder(BlendParent firstParent, BlendParent secondParent)
        {
            return string.CompareOrdinal(firstParent.SortKey, secondParent.SortKey) <= 0 ? (firstParent, secondParent) : (secondParent, firstParent);
        }

        // Expects parents already in canonical order
        public static string BuildPairKey(string seedId, BlendParent first, BlendParent second)
        {
            return $"{seedId}|{first.SortKey}+{second.SortKey}";
        }

        // Seeded by the save, day, player, pair and that pair's attempt today, so each splice rolls fresh but reloading the day replays the same results
        public static Random Create(string pairKey, int attemptToday)
        {
            return Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, Game1.player.UniqueMultiplayerID, Game1.hash.GetDeterministicHashCode(pairKey), attemptToday);
        }
    }
}
