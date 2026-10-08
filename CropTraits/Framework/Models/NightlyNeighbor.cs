using Microsoft.Xna.Framework;

namespace CropTraits.Framework.Models
{
    // A crop in one of the eight tiles around a traited crop, with its own plan so neighbor effects can change it
    public record NightlyNeighbor(Vector2 Tile, TileTraitState State, NightlyGrowthPlan Plan);
}
