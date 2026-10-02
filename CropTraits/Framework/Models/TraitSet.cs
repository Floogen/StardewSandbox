using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Models
{
    // Trait IDs mapped to tiers, stored as a compact canonical key (e.g. Prolific:2|Swift:1)
    public class TraitSet
    {
        public const char TRAIT_SEPARATOR = '|';
        public const char TIER_SEPARATOR = ':';

        // Ordinal sorting makes iteration order, and so every random draw, deterministic by construction
        private readonly SortedDictionary<string, int> _traits = new(StringComparer.Ordinal);

        public int Count => _traits.Count;
        public IEnumerable<string> Ids => _traits.Keys;
        public IEnumerable<KeyValuePair<string, int>> Entries => _traits;

        public int GetTier(string traitId)
        {
            if (string.IsNullOrEmpty(traitId))
            {
                return 0;
            }

            return _traits.TryGetValue(traitId, out int tier) ? tier : 0;
        }

        public bool Has(string traitId)
        {
            return string.IsNullOrEmpty(traitId) is false && _traits.ContainsKey(traitId);
        }

        public void Set(string traitId, int tier)
        {
            if (string.IsNullOrEmpty(traitId))
            {
                return;
            }

            _traits[traitId] = Math.Max(tier, 1);
        }

        public bool Remove(string traitId)
        {
            return string.IsNullOrEmpty(traitId) is false && _traits.Remove(traitId);
        }

        public TraitSet Clone()
        {
            TraitSet clone = new TraitSet();
            foreach (KeyValuePair<string, int> trait in _traits)
            {
                clone.Set(trait.Key, trait.Value);
            }

            return clone;
        }

        // The key is used for stacking, blend seeding and the blend journal, so its format must never change
        public string ToKey()
        {
            return string.Join(TRAIT_SEPARATOR, _traits.Select(trait => $"{trait.Key}{TIER_SEPARATOR}{trait.Value}"));
        }

        public static TraitSet FromKey(string traitKey)
        {
            TraitSet traitSet = new TraitSet();
            if (string.IsNullOrWhiteSpace(traitKey))
            {
                return traitSet;
            }

            foreach (string entry in traitKey.Split(TRAIT_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] parts = entry.Split(TIER_SEPARATOR);
                if (parts.Length == 2 && string.IsNullOrEmpty(parts[0]) is false && int.TryParse(parts[1], out int tier))
                {
                    traitSet.Set(parts[0], tier);
                }
            }

            return traitSet;
        }
    }
}
