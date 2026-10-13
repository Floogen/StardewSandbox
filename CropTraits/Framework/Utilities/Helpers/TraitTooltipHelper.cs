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
        public static readonly Color NEW_TRAIT_COLOR = Color.ForestGreen;

        public static int GetHeight(Item item, SpriteFont font)
        {
            List<List<(string Text, Color Color)>> lines = BuildLines(item, font);
            return lines.Count == 0 ? 0 : ((lines.Count + 1) * font.LineSpacing) + GetTrailingGap(item, font);
        }

        public static int GetTrailingGap(Item item, SpriteFont font)
        {
            return item is StardewValley.Object obj && obj.Edibility != StardewValley.Object.inedible ? font.LineSpacing : TEXT_OFFSET_Y;
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
                DrawLine(spriteBatch, line, new Vector2(x + TEXT_OFFSET_X, y + TEXT_OFFSET_Y), font, alpha);
                y += font.LineSpacing;
            }

            y += GetTrailingGap(item, font);
        }

        // Draws one line's colored segments side by side
        public static void DrawLine(SpriteBatch spriteBatch, List<(string Text, Color Color)> line, Vector2 position, SpriteFont font, float alpha)
        {
            float drawX = position.X;
            foreach ((string text, Color color) in line)
            {
                Utility.drawTextWithShadow(spriteBatch, text, font, new Vector2(drawX, position.Y), color * alpha);
                drawX += font.MeasureString(text).X;
            }
        }

        public static float GetLineWidth(List<(string Text, Color Color)> line, SpriteFont font)
        {
            return line.Sum(segment => font.MeasureString(segment.Text).X);
        }

        public static List<List<(string Text, Color Color)>> BuildLines(Item item, SpriteFont font)
        {
            TraitSet traits = item is null ? new TraitSet() : TraitModDataHelper.GetTraits(item.modData);
            return BuildLines(traits, null, font, LINE_WIDTH);
        }

        /// <summary>
        /// Builds the "Traits:" line as colored segments, wrapped by whole trait so a name and its marker never split across lines.
        /// Traits in newTraitIds get a "New!" marker after them, which the crop tooltip uses for mutations.
        /// </summary>
        public static List<List<(string Text, Color Color)>> BuildLines(TraitSet traits, ISet<string> newTraitIds, SpriteFont font, int lineWidth)
        {
            List<List<(string Text, Color Color)>> lines = new();
            if (traits is null || traits.Count == 0)
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

                List<(string Text, Color Color)> segments = new() { (CropTraits.traitManager.GetTierLabel(trait.Key, trait.Value), isNegative ? NEGATIVE_TRAIT_COLOR : Game1.textColor) };
                if (newTraitIds is not null && newTraitIds.Contains(trait.Key))
                {
                    segments.Add(($" {TranslationHelper.Get("tooltip.new_trait")}", NEW_TRAIT_COLOR));
                }

                if (index < orderedTraits.Count - 1)
                {
                    segments.Add((SEPARATOR, Game1.textColor));
                }

                float segmentsWidth = GetLineWidth(segments, font);
                if (currentWidth + segmentsWidth > lineWidth)
                {
                    lines.Add(currentLine);
                    currentLine = new();
                    currentWidth = 0;
                }

                currentLine.AddRange(segments);
                currentWidth += segmentsWidth;
            }

            lines.Add(currentLine);
            return lines;
        }

        // Plain text trait list
        public static string GetTraitsText(TraitSet traits)
        {
            if (traits is null || traits.Count == 0)
            {
                return string.Empty;
            }

            return $"{TranslationHelper.Get("tooltip.traits_label")} {CropTraits.traitManager.GetLabel(traits.ToKey())}";
        }

        // An item's own description followed by its traits
        public static string GetDescriptionWithTraits(Item item)
        {
            string description = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId).Description;
            string traitsText = GetTraitsText(TraitModDataHelper.GetTraits(item.modData));

            return string.IsNullOrEmpty(traitsText) ? description : $"{description}\n\n{traitsText}";
        }
    }
}
