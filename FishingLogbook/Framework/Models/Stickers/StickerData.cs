using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;

namespace FishingLogbook.Framework.Models.Stickers
{
    public class StickerData
    {
        /// <summary>The qualified item ID this sticker was generated from.</summary>
        public string Id { get; set; }

        /// <summary>The asset name the generated sticker texture is served under.</summary>
        public string TexturePath { get; set; }

        /// <summary>The asset name of the source sprite sheet.</summary>
        public string SourceAssetName { get; set; }

        /// <summary>The region of the source sprite sheet to outline. This is the unpadded sprite, so it is not what you draw with.</summary>
        public Rectangle SheetSourceRectangle { get; set; }

        /// <summary>The non-premultiplied color to use for the outline.</summary>
        public Color OutlineColor { get; set; }

        /// <summary>The outline thickness, in source pixels.</summary>
        public int OutlineThickness { get; set; } = 1;

        /// <summary>Whether diagonal neighbors count, which squares off corners instead of following them.</summary>
        public bool IncludeDiagonals { get; set; }

        /// <summary>The minimum alpha for a source pixel to count as solid.</summary>
        public byte AlphaThreshold { get; set; } = 1;

        private Color[] _pixels;

        /// <summary>Whether the outlined pixel data has been generated.</summary>
        public bool IsGenerated => _pixels is not null;

        /// <summary>
        /// The dimensions of the sticker, which include the outline padding on all four sides.
        /// This is derived from the source region, so it is valid before the pixel data has been generated.
        /// </summary>
        public Point Size => new Point(this.SheetSourceRectangle.Width + this.OutlineThickness * 2, this.SheetSourceRectangle.Height + this.OutlineThickness * 2);

        public (bool Result, string Error) IsValid()
        {
            if (String.IsNullOrEmpty(this.Id))
            {
                return (false, "Missing required property: Id");
            }

            if (String.IsNullOrEmpty(this.TexturePath))
            {
                return (false, "Missing required property: TexturePath");
            }

            if (String.IsNullOrEmpty(this.SourceAssetName))
            {
                return (false, "Missing required property: SourceAssetName");
            }

            if (this.SheetSourceRectangle.Width <= 0 || this.SheetSourceRectangle.Height <= 0)
            {
                return (false, $"SheetSourceRectangle ({this.SheetSourceRectangle}) must have a positive width and height");
            }

            if (this.OutlineThickness < 0)
            {
                return (false, $"OutlineThickness ({this.OutlineThickness}) cannot be negative");
            }

            return (true, String.Empty);
        }

        /// <summary>
        /// Stores the generated pixel data for this sticker.
        /// </summary>
        public void SetPixels(Color[] pixels)
        {
            _pixels = pixels;
        }

        /// <summary>
        /// Creates a new texture from the generated pixel data. The caller owns the returned texture.
        /// </summary>
        /// <returns>The generated texture, or null if this sticker has not been generated yet</returns>
        public Texture2D CreateTexture()
        {
            if (this.IsGenerated is false)
            {
                return null;
            }

            Point size = this.Size;

            Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, size.X, size.Y);
            texture.SetData(_pixels);

            return texture;
        }
    }
}
