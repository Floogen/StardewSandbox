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
        public const float BADGE_SCALE = 3f; // Matches the quality star's scale
        public const float BADGE_MARGIN = 4f;
        public const float BADGE_LAYER_OFFSET = 0.0001f;

        public static void DrawBadge(Object item, SpriteBatch spriteBatch, Vector2 location, float slotScaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber)
        {
            // Hidden alongside the stack number and quality star, which also keeps faded previews (like a running splice) from giving anything away
            if (drawStackNumber == StackDrawType.Hide || IsTraitedCrop(item) is false)
            {
                return;
            }

            Vector2 origin = new Vector2(AssetManager.MUTATION_BADGE_SOURCE.Width / 2f, AssetManager.MUTATION_BADGE_SOURCE.Height / 2f);
            Vector2 badgeCenter = location + new Vector2(Game1.tileSize - BADGE_MARGIN - (origin.X * BADGE_SCALE), BADGE_MARGIN + (origin.Y * BADGE_SCALE));

            Texture2D texture = Game1.content.Load<Texture2D>(AssetManager.SPRITE_SHEET_TEXTURE_PATH);
            spriteBatch.Draw(texture, badgeCenter, AssetManager.MUTATION_BADGE_SOURCE, Color.White * transparency, 0f, origin, BADGE_SCALE * slotScaleSize, SpriteEffects.None, layerDepth + BADGE_LAYER_OFFSET);
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
