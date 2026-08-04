using Microsoft.Xna.Framework;

namespace FishingLogbook.Framework.Models.Stickers
{
    /// <summary>
    /// A single sticker positioned within a layout area at a given scale.
    /// </summary>
    public class StickerPlacement
    {
        /// <summary>The sticker being placed.</summary>
        public StickerData Sticker { get; set; }

        /// <summary>The top-left corner of the sticker, in screen pixels. This includes the outline padding.</summary>
        public Vector2 Position { get; set; }

        /// <summary>The scale the sticker is drawn at.</summary>
        public int Scale { get; set; }

        /// <summary>Whether or not the sticker should render flipped horizontally.</summary>
        public bool IsFlipped { get; set; }

        /// <summary>Whether this sticker had to overlap an already placed sticker, because no empty spot remained.</summary>
        public bool IsOverlapping { get; set; }

        /// <summary>The size of the sticker on screen, including the outline padding.</summary>
        public Point Footprint => new Point(this.Sticker.Size.X * this.Scale, this.Sticker.Size.Y * this.Scale);

        /// <summary>The area this sticker occupies on screen, including the outline padding.</summary>
        public Rectangle Bounds => new Rectangle((int)this.Position.X, (int)this.Position.Y, this.Footprint.X, this.Footprint.Y);
    }
}
