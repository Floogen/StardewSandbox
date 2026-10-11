using CropTraits.Framework.Models;
using StardewValley.Mods;
using System.Collections.Generic;

namespace CropTraits.Framework.Utilities.Helpers
{
    // Reads and writes trait sets on modData for items and HoeDirt
    public static class TraitModDataHelper
    {
        // Each trait key is parsed once, since some traits are read every frame. The sets are shared, so callers must never change them
        private static readonly Dictionary<string, TraitSet> _traitsByKey = new();
        private static readonly TraitSet _emptyTraits = new();

        public static TraitSet GetCachedTraits(string traitKey)
        {
            if (string.IsNullOrEmpty(traitKey))
            {
                return _emptyTraits;
            }

            if (_traitsByKey.TryGetValue(traitKey, out TraitSet traits) is false)
            {
                traits = TraitSet.FromKey(traitKey);
                _traitsByKey[traitKey] = traits;
            }

            return traits;
        }

        public static string GetTraitKey(ModDataDictionary modData)
        {
            if (modData is null || modData.TryGetValue(CropTraits.TRAITS_MOD_DATA_KEY, out string traitKey) is false)
            {
                return string.Empty;
            }

            return traitKey ?? string.Empty;
        }

        public static TraitSet GetTraits(ModDataDictionary modData)
        {
            return TraitSet.FromKey(GetTraitKey(modData));
        }

        public static bool HasTraits(ModDataDictionary modData)
        {
            return string.IsNullOrEmpty(GetTraitKey(modData)) is false;
        }

        // Empty sets remove the key so untraited items keep stacking as in vanilla
        public static void SetTraits(ModDataDictionary modData, TraitSet traits)
        {
            string traitKey = traits?.ToKey();
            if (string.IsNullOrEmpty(traitKey))
            {
                modData.Remove(CropTraits.TRAITS_MOD_DATA_KEY);
                return;
            }

            modData[CropTraits.TRAITS_MOD_DATA_KEY] = traitKey;
        }
    }
}
