using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class CropTooltipHelper
    {
        public const int LINE_WIDTH = 320;
        public const int PADDING = 20;
        public const int TITLE_GAP = 4;
        public const int CURSOR_OFFSET = 32;

        public static readonly Rectangle BOX_SOURCE = new Rectangle(0, 256, 60, 60);

        public static void Draw(SpriteBatch spriteBatch, HoeDirt dirt, Vector2 tile)
        {
            TraitSet traits = PlantedTraitHelper.GetVisibleTraits(dirt);
            if (traits.Count == 0)
            {
                return;
            }

            SpriteFont titleFont = Game1.dialogueFont;
            SpriteFont font = Game1.smallFont;
            string title = PlantedTraitHelper.GetCropName(dirt.crop);

            List<List<(string Text, Color Color)>> lines = TraitTooltipHelper.BuildLines(traits, PlantedTraitHelper.GetNewTraitIds(dirt), font, LINE_WIDTH);

            if (PlantedTraitHelper.IsMature(dirt.crop) is false)
            {
                lines.Add(new List<(string Text, Color Color)>());
                foreach (string noteLine in Game1.parseText(TranslationHelper.Get("tooltip.mutation_hidden"), font, LINE_WIDTH).Split('\n'))
                {
                    lines.Add(new List<(string Text, Color Color)> { (noteLine, Game1.textColor) });
                }
            }

            Vector2 titleSize = titleFont.MeasureString(title);
            float linesWidth = lines.Count == 0 ? 0 : lines.Max(line => TraitTooltipHelper.GetLineWidth(line, font));
            int width = (int)Math.Ceiling(Math.Max(titleSize.X, linesWidth)) + (PADDING * 2);
            int height = (int)Math.Ceiling(titleSize.Y) + TITLE_GAP + (lines.Count * font.LineSpacing) + (PADDING * 2);

            Point position = GetPosition(tile, width, height);
            IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, BOX_SOURCE, position.X, position.Y, width, height, Color.White);

            Vector2 textPosition = new Vector2(position.X + PADDING, position.Y + PADDING);
            Utility.drawTextWithShadow(spriteBatch, title, titleFont, textPosition, Game1.textColor);
            textPosition.Y += titleSize.Y + TITLE_GAP;

            foreach (List<(string Text, Color Color)> line in lines)
            {
                TraitTooltipHelper.DrawLine(spriteBatch, line, textPosition, font, 1f);
                textPosition.Y += font.LineSpacing;
            }
        }

        public static Point GetPosition(Vector2 tile, int width, int height)
        {
            Vector2 anchor = Game1.lastCursorMotionWasMouse ? new Vector2(Game1.getMouseX(true), Game1.getMouseY(true)) : Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(Game1.viewport, tile * Game1.tileSize));

            int x = (int)anchor.X + CURSOR_OFFSET;
            int y = (int)anchor.Y + CURSOR_OFFSET;

            // Flipped to the other side of the anchor when it would run off the screen
            if (x + width > Game1.uiViewport.Width)
            {
                x = (int)anchor.X - width;
            }

            if (y + height > Game1.uiViewport.Height)
            {
                y = (int)anchor.Y - height;
            }

            return new Point(Math.Max(0, x), Math.Max(0, y));
        }
    }
}
