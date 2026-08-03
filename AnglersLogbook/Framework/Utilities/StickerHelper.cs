using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace AnglersLogbook.Framework.Utilities
{
    /// <summary>
    /// Handles sticker pixel generation and the random sources which drive sticker selection and layout.
    /// </summary>
    public static class StickerHelper
    {
        /// <summary>
        /// Creates a random source which is fixed for the current day.
        /// </summary>
        /// <param name="salt">An extra value to vary the sequence, so separate features do not roll alike</param>
        public static Random CreateDayRandom(int salt = 0)
        {
            return Utility.CreateDaySaveRandom(salt);
        }

        /// <summary>
        /// Creates a random source which is fixed for the given sticker on the current day.
        /// </summary>
        public static Random CreateStickerRandom(string qualifiedItemId, int salt = 0)
        {
            return Utility.CreateDaySaveRandom(Game1.hash.GetDeterministicHashCode(qualifiedItemId), salt);
        }

        /// <summary>
        /// Creates a padded copy of the given source region, with an outline drawn around its solid pixels.
        /// </summary>
        /// <param name="sheetPixels">The full pixel buffer of the source sprite sheet</param>
        /// <param name="sheetWidth">The width of the source sprite sheet, in pixels</param>
        /// <param name="sheetHeight">The height of the source sprite sheet, in pixels</param>
        /// <param name="sourceRectangle">The region of the source sprite sheet to outline</param>
        /// <param name="outlineColor">The non-premultiplied color to use for the outline</param>
        /// <param name="thickness">The outline thickness, in source pixels</param>
        /// <param name="includeDiagonals">Whether diagonal neighbors count, which squares off corners instead of following them</param>
        /// <param name="alphaThreshold">The minimum alpha for a source pixel to count as solid</param>
        /// <param name="pixels">The generated pixel buffer, sized (width + thickness * 2) by (height + thickness * 2)</param>
        /// <param name="size">The dimensions of the generated pixel buffer</param>
        /// <returns>Whether the generation succeeded, along with the reason for any failure</returns>
        public static (bool Result, string Error) TryCreateOutlinedPixels(Color[] sheetPixels, int sheetWidth, int sheetHeight, Rectangle sourceRectangle, Color outlineColor, int thickness, bool includeDiagonals, byte alphaThreshold, out Color[] pixels, out Point size)
        {
            pixels = Array.Empty<Color>();
            size = Point.Zero;

            if (sheetPixels is null)
            {
                return (false, "The given sheet pixel buffer was null");
            }

            if (sheetWidth <= 0 || sheetHeight <= 0)
            {
                return (false, $"The given sheet dimensions ({sheetWidth}x{sheetHeight}) must be positive");
            }

            if (sheetPixels.Length != sheetWidth * sheetHeight)
            {
                return (false, $"The given sheet pixel buffer (length {sheetPixels.Length}) does not match the given dimensions ({sheetWidth}x{sheetHeight})");
            }

            if (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0)
            {
                return (false, $"The given source rectangle ({sourceRectangle}) must have a positive width and height");
            }

            if (sourceRectangle.Left < 0 || sourceRectangle.Top < 0 || sourceRectangle.Right > sheetWidth || sourceRectangle.Bottom > sheetHeight)
            {
                return (false, $"The given source rectangle ({sourceRectangle}) falls outside the sheet bounds ({sheetWidth}x{sheetHeight})");
            }

            if (thickness < 0)
            {
                return (false, $"The given outline thickness ({thickness}) cannot be negative");
            }

            int paddedWidth = sourceRectangle.Width + thickness * 2, paddedHeight = sourceRectangle.Height + thickness * 2;
            Color[] target = new Color[paddedWidth * paddedHeight];
            Color premultiplied = Color.FromNonPremultiplied(outlineColor.R, outlineColor.G, outlineColor.B, outlineColor.A);

            for (int y = 0; y < paddedHeight; y++)
            {
                for (int x = 0; x < paddedWidth; x++)
                {
                    // these are relative to the source rectangle, so they go negative across the padding
                    int localX = x - thickness, localY = y - thickness;

                    if (IsSolid(sheetPixels, sheetWidth, sourceRectangle, localX, localY, alphaThreshold))
                    {
                        target[x + y * paddedWidth] = sheetPixels[(sourceRectangle.X + localX) + (sourceRectangle.Y + localY) * sheetWidth];
                    }
                    else if (HasSolidNeighbor(sheetPixels, sheetWidth, sourceRectangle, localX, localY, thickness, includeDiagonals, alphaThreshold))
                    {
                        target[x + y * paddedWidth] = premultiplied;
                    }
                }
            }

            pixels = target;
            size = new Point(paddedWidth, paddedHeight);

            return (true, String.Empty);
        }

        /// <summary>
        /// Determines whether the pixel at the given position (relative to the source rectangle) is solid enough to outline.
        /// </summary>
        private static bool IsSolid(Color[] sheetPixels, int sheetWidth, Rectangle sourceRectangle, int localX, int localY, byte alphaThreshold)
        {
            // clamping to the source rectangle stops neighboring sprites on the sheet from bleeding into the outline
            if (localX < 0 || localY < 0 || localX >= sourceRectangle.Width || localY >= sourceRectangle.Height)
            {
                return false;
            }

            return sheetPixels[(sourceRectangle.X + localX) + (sourceRectangle.Y + localY) * sheetWidth].A >= alphaThreshold;
        }

        /// <summary>
        /// Determines whether any pixel within the given thickness of the given position is solid.
        /// </summary>
        private static bool HasSolidNeighbor(Color[] sheetPixels, int sheetWidth, Rectangle sourceRectangle, int localX, int localY, int thickness, bool includeDiagonals, byte alphaThreshold)
        {
            for (int offsetY = -thickness; offsetY <= thickness; offsetY++)
            {
                for (int offsetX = -thickness; offsetX <= thickness; offsetX++)
                {
                    if (offsetX is 0 && offsetY is 0)
                    {
                        continue;
                    }

                    if (includeDiagonals is false && Math.Abs(offsetX) + Math.Abs(offsetY) > thickness)
                    {
                        continue;
                    }

                    if (IsSolid(sheetPixels, sheetWidth, sourceRectangle, localX + offsetX, localY + offsetY, alphaThreshold))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
