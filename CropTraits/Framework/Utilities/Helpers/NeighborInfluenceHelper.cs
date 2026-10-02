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
        public const int MAX_NEIGHBOR_BONUS = 10;

        // Same-crop neighbors in all eight surrounding tiles bias which trait is rolled
        public static Dictionary<string, int> GetWeightBonuses(Vector2 tile, Dictionary<Vector2, TileTraitState> snapshot)
        {
            Dictionary<string, int> weightBonuses = new();
            if (snapshot.TryGetValue(tile, out TileTraitState centerState) is false)
            {
                return weightBonuses;
            }

            foreach (Vector2 neighborTile in Utility.getSurroundingTileLocationsArray(tile))
            {
                if (snapshot.TryGetValue(neighborTile, out TileTraitState neighborState) is false || neighborState.CropId != centerState.CropId)
                {
                    continue;
                }

                foreach (KeyValuePair<string, int> trait in neighborState.Traits.Entries)
                {
                    int currentBonus = weightBonuses.GetValueOrDefault(trait.Key);
                    weightBonuses[trait.Key] = Math.Min(currentBonus + (NEIGHBOR_WEIGHT_BONUS * trait.Value), MAX_NEIGHBOR_BONUS);
                }
            }

            return weightBonuses;
        }
    }
}
