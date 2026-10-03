using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class CropSparkleHelper
    {
        public const int PIXEL_SCALE = 4;
        public const int CROP_ORIGIN_X = 8;
        public const int CROP_ORIGIN_Y = 24;
        public const int POT_OFFSET_Y = -24; // Garden pots draw their crop higher than ground dirt
        public const byte VISIBLE_ALPHA_THRESHOLD = 0;
        public const int EDGE_INSET_PIXELS = 1;
        public const int SPARKLE_SIZE = 64;
        public const int SPARKLE_FIRST_ROW = 10;
        public const int SPARKLE_FRAME_COUNT = 8;
        public const float SPARKLE_FRAME_INTERVAL = 100f;
        public const float LAYER_DEPTH_PER_PIXEL = 1f / 10000f;
        public const float SPARKLE_LAYER_OFFSET = 1f / 10000f;

        /// <summary>
        /// Returns the frame pixels a sparkle can be centered on, relative to the frame's top left.
        /// Only pixels whose neighbors within EDGE_INSET_PIXELS are also visible are used, unless the crop is too thin to have any, in which case every visible pixel is used.
        /// An empty list means the frame is fully transparent.
        /// </summary>
        public static List<Point> GetSparklePoints(Texture2D texture, Rectangle sourceRect)
        {
            List<Point> visiblePoints = new();
            List<Point> interiorPoints = new();

            Rectangle area = Rectangle.Intersect(sourceRect, texture.Bounds);
            if (area.Width <= 0 || area.Height <= 0)
            {
                return visiblePoints;
            }

            Color[] pixels = new Color[area.Width * area.Height];
            texture.GetData(0, area, pixels, 0, pixels.Length);

            for (int y = 0; y < area.Height; y++)
            {
                for (int x = 0; x < area.Width; x++)
                {
                    if (IsVisible(pixels, area, x, y) is false)
                    {
                        continue;
                    }

                    Point framePoint = new Point(x + (area.X - sourceRect.X), y + (area.Y - sourceRect.Y));
                    visiblePoints.Add(framePoint);

                    if (IsInterior(pixels, area, x, y))
                    {
                        interiorPoints.Add(framePoint);
                    }
                }
            }

            return interiorPoints.Count > 0 ? interiorPoints : visiblePoints;
        }

        // Every pixel of the frame, for when the texture can't be read
        public static List<Point> GetAllPoints(Rectangle sourceRect)
        {
            List<Point> points = new();
            for (int y = 0; y < sourceRect.Height; y++)
            {
                for (int x = 0; x < sourceRect.Width; x++)
                {
                    points.Add(new Point(x, y));
                }
            }

            return points;
        }

        public static bool IsVisible(Color[] pixels, Rectangle area, int x, int y)
        {
            if (x < 0 || y < 0 || x >= area.Width || y >= area.Height)
            {
                return false;
            }

            return pixels[(y * area.Width) + x].A > VISIBLE_ALPHA_THRESHOLD;
        }

        public static bool IsInterior(Color[] pixels, Rectangle area, int x, int y)
        {
            for (int offsetY = -EDGE_INSET_PIXELS; offsetY <= EDGE_INSET_PIXELS; offsetY++)
            {
                for (int offsetX = -EDGE_INSET_PIXELS; offsetX <= EDGE_INSET_PIXELS; offsetX++)
                {
                    if (IsVisible(pixels, area, x + offsetX, y + offsetY) is false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static Vector2 GetWorldPosition(Point framePoint, int frameWidth, Vector2 tile, bool isFlipped, bool isInPot, Random random)
        {
            int frameX = isFlipped ? frameWidth - 1 - framePoint.X : framePoint.X;
            Vector2 anchor = (tile * Game1.tileSize) + new Vector2(Game1.tileSize / 2f, (Game1.tileSize / 2f) + (isInPot ? POT_OFFSET_Y : 0));

            return anchor + new Vector2(((frameX - CROP_ORIGIN_X) * PIXEL_SCALE) + random.Next(PIXEL_SCALE), ((framePoint.Y - CROP_ORIGIN_Y) * PIXEL_SCALE) + random.Next(PIXEL_SCALE));
        }

        public static float GetLayerDepth(Vector2 tile)
        {
            return (((tile.Y * Game1.tileSize) + (Game1.tileSize / 2f)) * LAYER_DEPTH_PER_PIXEL) + SPARKLE_LAYER_OFFSET;
        }

        public static void AddSparkles(GameLocation location, Func<Vector2> getSparkleCenter, float layerDepth, int totalDurationMilliseconds, int millisecondsBetweenSparkles, Color color, Random random)
        {
            int sparkleCount = Math.Max(1, totalDurationMilliseconds / millisecondsBetweenSparkles);
            for (int index = 0; index < sparkleCount; index++)
            {
                Vector2 position = getSparkleCenter() - new Vector2(SPARKLE_SIZE / 2f, SPARKLE_SIZE / 2f);

                location.temporarySprites.Add(new TemporaryAnimatedSprite(random.Next(SPARKLE_FIRST_ROW, SPARKLE_FIRST_ROW + 2), position, color, SPARKLE_FRAME_COUNT, false, SPARKLE_FRAME_INTERVAL)
                {
                    layerDepth = layerDepth,
                    delayBeforeAnimationStart = millisecondsBetweenSparkles * index
                });
            }
        }
    }
}
