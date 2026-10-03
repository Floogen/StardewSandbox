using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    /// <summary>
    /// Draws the "Traits:" line under an item's tooltip description, with negative traits in red.
    /// A description is a single string drawn in one color, so the line is drawn through the item tooltip hooks instead.
    /// </summary>
    public static class TraitTooltipHelper
    {
        public const int LINE_WIDTH = 272;
        public const int TEXT_OFFSET_X = 16;
        public const int TEXT_OFFSET_Y = 20;
        public const string SEPARATOR = ", ";

        public static readonly Color NEGATIVE_TRAIT_COLOR = Color.Red;

        public static int GetHeight(Item item, SpriteFont font)
        {
            List<List<(string Text, Color Color)>> lines = BuildLines(item, font);
            return lines.Count == 0 ? 0 : ((lines.Count + 1) * font.LineSpacing) + GetTrailingGap(item, font);
        }

        public static int GetTrailingGap(Item item, SpriteFont font)
        {
            return item is StardewValley.Object obj && obj.Edibility != StardewValley.Object.inedible ? font.LineSpacing : 0;
        }

        public static void DrawTraits(Item item, SpriteBatch spriteBatch, int x, ref int y, SpriteFont font, float alpha)
        {
            List<List<(string Text, Color Color)>> lines = BuildLines(item, font);
            if (lines.Count == 0)
            {
                return;
            }

            // Blank line between the description and the traits
            y += font.LineSpacing;

            foreach (List<(string Text, Color Color)> line in lines)
            {
                float drawX = x + TEXT_OFFSET_X;
                foreach ((string text, Color color) in line)
                {
                    Utility.drawTextWithShadow(spriteBatch, text, font, new Vector2(drawX, y + TEXT_OFFSET_Y), color * alpha);
                    drawX += font.MeasureString(text).X;
                }

                y += font.LineSpacing;
            }

            y += GetTrailingGap(item, font);
        }

        public static List<List<(string Text, Color Color)>> BuildLines(Item item, SpriteFont font)
        {
            List<List<(string Text, Color Color)>> lines = new();
            TraitSet traits = item is null ? new TraitSet() : TraitModDataHelper.GetTraits(item.modData);
            if (traits.Count == 0)
            {
                return lines;
            }

            List<KeyValuePair<string, int>> orderedTraits = traits.Entries
                .OrderBy(trait => CropTraits.traitManager.GetTraitName(trait.Key), StringComparer.CurrentCulture)
                .ToList();

            List<(string Text, Color Color)> currentLine = new() { ($"{TranslationHelper.Get("tooltip.traits_label")} ", Game1.textColor) };
            float currentWidth = font.MeasureString(currentLine[0].Text).X;

            for (int index = 0; index < orderedTraits.Count; index++)
            {
                KeyValuePair<string, int> trait = orderedTraits[index];
                bool isNegative = CropTraits.traitManager.TryGetTrait(trait.Key, out Trait definition) && definition.IsNegative;

                string text = CropTraits.traitManager.GetTierLabel(trait.Key, trait.Value);
                if (index < orderedTraits.Count - 1)
                {
                    text = $"{text}{SEPARATOR}";
                }

                float textWidth = font.MeasureString(text).X;
                if (currentWidth + textWidth > LINE_WIDTH && currentLine.Count > 0)
                {
                    lines.Add(currentLine);
                    currentLine = new();
                    currentWidth = 0;
                }

                currentLine.Add((text, isNegative ? NEGATIVE_TRAIT_COLOR : Game1.textColor));
                currentWidth += textWidth;
            }

            lines.Add(currentLine);
            return lines;
        }
    }
}
