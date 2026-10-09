using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;

namespace CropTraits.Framework.Models
{
    // Everything a trait needs at the start of the day, with the random seeded by day, save and tile so every roll is repeatable
    public record MorningContext(GameLocation Location, Vector2 Tile, HoeDirt Dirt, TraitSet Traits, Random Random, MonsterSpawnLimit MonsterSpawnLimit);
}
