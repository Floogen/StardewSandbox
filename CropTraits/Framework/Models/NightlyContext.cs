using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Models
{
    // Everything a trait needs at the end of the day, with the random seeded by day, save and tile so every roll is repeatable
    public record NightlyContext(GameLocation Location, Vector2 Tile, HoeDirt Dirt, TileTraitState State, NightlyGrowthPlan Plan, Random Random, bool IsOutdoors, bool IsRaining, IReadOnlyList<NightlyNeighbor> Neighbors);
}
