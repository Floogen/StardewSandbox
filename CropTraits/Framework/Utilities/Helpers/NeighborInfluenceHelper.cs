using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class NeighborInfluenceHelper
    {
        public const int NEIGHBOR_WEIGHT_BONUS = 2;
        public const int MAX_NEIGHBOR_BONUS = 24;

        // Same-crop neighbors in all eight surrounding tiles bias which trait is rolled
        public static Dictionary<string, int> GetWeightBonuses(Vector2 tile, Dictionary<Vector2, TileTraitState> snapshot)
        {
            Dictionary<string, int> weightBonuses = new();
            if (snapshot.TryGetValue(tile, out TileTraitState centerState) is false)
            {
                return weightBonuses;
            }

            // Totals are capped once every neighbor is counted, so the order neighbors are visited never matters
            Dictionary<string, int> totalBonuses = new();
            Dictionary<string, int> capMultipliers = new();
            foreach (Vector2 neighborTile in Utility.getSurroundingTileLocationsArray(tile))
            {
                if (snapshot.TryGetValue(neighborTile, out TileTraitState neighborState) is false || neighborState.CropId != centerState.CropId)
                {
                    continue;
                }

                // Traits like Dominant raise the cap for every trait on the neighbor, rather than how hard each one pulls
                int capMultiplier = CropTraits.traitManager.GetNeighborBonusCapMultiplier(neighborState.Traits);
                foreach (KeyValuePair<string, int> trait in neighborState.Traits.Entries)
                {
                    totalBonuses[trait.Key] = totalBonuses.GetValueOrDefault(trait.Key) + (NEIGHBOR_WEIGHT_BONUS * trait.Value);
                    capMultipliers[trait.Key] = Math.Max(capMultipliers.GetValueOrDefault(trait.Key, 1), capMultiplier);
                }
            }

            foreach (KeyValuePair<string, int> totalBonus in totalBonuses)
            {
                weightBonuses[totalBonus.Key] = Math.Min(totalBonus.Value, MAX_NEIGHBOR_BONUS * capMultipliers[totalBonus.Key]);
            }

            return weightBonuses;
        }
    }
}
