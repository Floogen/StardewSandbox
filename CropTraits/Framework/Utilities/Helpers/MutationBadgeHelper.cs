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
        public const float BADGE_SCALE = 3f;
        public const float BADGE_MARGIN = 4f;
        public const float BADGE_LAYER_OFFSET = 0.0001f;

        public const float CROP_BADGE_SCALE = 4f;
        public const float CROP_BADGE_OFFSET_Y = 0f;
        public const float CROP_BADGE_BOB_HEIGHT = 4f;
        public const double CROP_BADGE_BOB_PERIOD_MILLISECONDS = 250.0;

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

        public static void DrawCropBadge(SpriteBatch spriteBatch, Vector2 tile)
        {
            float bob = CROP_BADGE_BOB_HEIGHT * (float)System.Math.Round(System.Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / CROP_BADGE_BOB_PERIOD_MILLISECONDS), 2);
            Vector2 origin = new Vector2(AssetManager.MUTATION_BADGE_SOURCE.Width / 2f, AssetManager.MUTATION_BADGE_SOURCE.Height / 2f);
            Vector2 worldPosition = (tile * Game1.tileSize) + new Vector2(Game1.tileSize / 2f, -72f + bob);

            Texture2D texture = Game1.content.Load<Texture2D>(AssetManager.SPRITE_SHEET_TEXTURE_PATH);
            spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, worldPosition), AssetManager.MUTATION_BADGE_SOURCE, Color.White, 0f, origin, CROP_BADGE_SCALE, SpriteEffects.None, 1f);
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
