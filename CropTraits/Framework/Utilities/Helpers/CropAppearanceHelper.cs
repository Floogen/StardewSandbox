using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class CropAppearanceHelper
    {
        // Each trait key is parsed once, since tints are looked up every frame
        private static readonly Dictionary<string, TraitSet> _traitsByKey = new();

        public static TraitSet GetCachedTraits(string traitKey)
        {
            if (string.IsNullOrEmpty(traitKey))
            {
                return new TraitSet();
            }

            if (_traitsByKey.TryGetValue(traitKey, out TraitSet traits) is false)
            {
                traits = TraitSet.FromKey(traitKey);
                _traitsByKey[traitKey] = traits;
            }

            return traits;
        }

        // Before maturity only the seed's traits are visible, so a hidden mutation doesn't show early
        public static TraitSet GetVisibleTraits(HoeDirt dirt)
        {
            if (dirt?.crop is null)
            {
                return new TraitSet();
            }

            string modDataKey = PlantedTraitHelper.IsMature(dirt.crop) ? CropTraits.TRAITS_MOD_DATA_KEY : CropTraits.SEED_TRAITS_MOD_DATA_KEY;
            return GetCachedTraits(dirt.modData.TryGetValue(modDataKey, out string traitKey) ? traitKey : string.Empty);
        }

        public static Color GetCropTint(Crop crop)
        {
            if (crop is null || crop.dead.Value || crop.Dirt is null)
            {
                return Color.White;
            }

            return CropTraits.traitManager.GetTint(GetVisibleTraits(crop.Dirt), crop.netSeedIndex.Value);
        }

        // Only produce is tinted (flowers keep their own colors), since seeds and artisan goods aren't the crop itself
        public static Color GetItemTint(Item item)
        {
            if (item is null || item is ColoredObject || item.modData.TryGetValue(CropTraits.SEED_ID_MOD_DATA_KEY, out string seedId) is false)
            {
                return Color.White;
            }

            return CropTraits.traitManager.GetTint(GetCachedTraits(TraitModDataHelper.GetTraitKey(item.modData)), seedId);
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
