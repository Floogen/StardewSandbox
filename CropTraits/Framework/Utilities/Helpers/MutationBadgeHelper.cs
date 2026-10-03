using CropTraits.Framework.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace CropTraits.Framework.Utilities.Helpers
{
    /// <summary>
    /// Marks traited seeds and produce in menus with a small badge in the top right corner of the icon.
    /// </summary>
    public static class MutationBadgeHelper
    {
        public const float BADGE_SCALE = 2f;
        public const float BADGE_MARGIN = 1f;

        public static void DrawBadge(Object item, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber)
        {
            if (drawStackNumber == StackDrawType.Hide || IsTraitedCrop(item) is false)
            {
                return;
            }

            float halfSize = (Game1.tileSize / 2f) * scaleSize;
            float badgeSize = AssetManager.MUTATION_BADGE_SOURCE.Width * BADGE_SCALE * scaleSize;
            Vector2 center = location + new Vector2(Game1.tileSize / 2f, Game1.tileSize / 2f);
            Vector2 badgePosition = center + new Vector2(halfSize - badgeSize - (BADGE_MARGIN * scaleSize), -halfSize + (BADGE_MARGIN * scaleSize));

            Texture2D texture = Game1.content.Load<Texture2D>(AssetManager.SPRITE_SHEET_TEXTURE_PATH);
            spriteBatch.Draw(texture, badgePosition, AssetManager.MUTATION_BADGE_SOURCE, Color.White * transparency, 0f, Vector2.Zero, BADGE_SCALE * scaleSize, SpriteEffects.None, layerDepth + 0.0001f);
        }

        public static bool IsTraitedCrop(Item item)
        {
            if (item is null || TraitModDataHelper.HasTraits(item.modData) is false)
            {
                return false;
            }

            return item.Category == Object.SeedsCategory || item.modData.ContainsKey(CropTraits.SEED_ID_MOD_DATA_KEY);
        }
    }
}
