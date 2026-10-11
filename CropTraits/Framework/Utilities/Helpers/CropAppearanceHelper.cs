using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class CropAppearanceHelper
    {
        // Called for every crop drawn each frame, so untraited crops leave before any trait is resolved
        public static Color GetCropTint(Crop crop)
        {
            if (crop is null || crop.dead.Value || crop.Dirt is null)
            {
                return Color.White;
            }

            TraitSet traits = PlantedTraitHelper.GetVisibleTraits(crop.Dirt);
            return traits.Count == 0 ? Color.White : CropTraits.traitManager.GetTint(traits, crop.netSeedIndex.Value);
        }

        // Only produce is tinted (flowers keep their own colors), since seeds and artisan goods aren't the crop itself
        public static Color GetItemTint(Item item)
        {
            if (item is null || item is ColoredObject || item.modData.TryGetValue(CropTraits.SEED_ID_MOD_DATA_KEY, out string seedId) is false)
            {
                return Color.White;
            }

            return CropTraits.traitManager.GetTint(TraitModDataHelper.GetCachedTraits(TraitModDataHelper.GetTraitKey(item.modData)), seedId);
        }

        public static Color Multiply(Color baseColor, Color tint)
        {
            if (tint == Color.White)
            {
                return baseColor;
            }

            return new Color(baseColor.R * tint.R / 255, baseColor.G * tint.G / 255, baseColor.B * tint.B / 255, baseColor.A * tint.A / 255);
        }
    }
}
