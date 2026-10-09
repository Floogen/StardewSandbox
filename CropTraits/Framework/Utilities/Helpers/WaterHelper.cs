using Microsoft.Xna.Framework;
using StardewValley;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class WaterHelper
    {
        // How far water can be from the crop in every direction, matching the reach the game gives paddy crops
        public const int WATER_RANGE = 3;

        // Any water tile counts (ponds, rivers and the ocean), while fish ponds and troughs are buildings rather than water tiles
        public static bool IsNearWater(GameLocation location, Vector2 tile)
        {
            if (location is null)
            {
                return false;
            }

            int tileX = (int)tile.X;
            int tileY = (int)tile.Y;
            for (int offsetX = -WATER_RANGE; offsetX <= WATER_RANGE; offsetX++)
            {
                for (int offsetY = -WATER_RANGE; offsetY <= WATER_RANGE; offsetY++)
                {
                    if (location.isWaterTile(tileX + offsetX, tileY + offsetY))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
