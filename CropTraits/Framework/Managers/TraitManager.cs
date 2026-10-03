using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Managers
{
    public class TraitManager : BaseManager
    {
        public const int MAX_TRAITS = 3;
        public const double MIN_PRICE_MULTIPLIER = 0.10;

        private static readonly string[] ROMAN_NUMERALS = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        private Dictionary<string, Trait> _traits = new();
        private List<CombinationData> _combinations = new();
        private List<Trait> _rollableTraits = new();

        public IReadOnlyDictionary<string, Trait> Traits => _traits;
        public IReadOnlyList<CombinationData> Combinations => _combinations;

        /// <summary>Handles the trait and combination definitions, trait rules and display labels.</summary>
        public TraitManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            LoadDefinitions();
        }

        public void LoadDefinitions()
        {
            _traits = DiscoverTraits();
            _combinations = GetDefaultCombinations();

            // Sorted by ID so weighted rolls are deterministic for a given seed
            _rollableTraits = _traits.Values
                .Where(trait => trait.CanBeRolled)
                .OrderBy(trait => trait.Id, StringComparer.Ordinal)
                .ToList();
        }

        public int GetMaxTier(string traitId)
        {
            return TryGetTrait(traitId, out Trait trait) ? trait.MaxTier : 1;
        }

        public bool TryGetTrait(string traitId, out Trait trait)
        {
            trait = null;
            if (string.IsNullOrEmpty(traitId))
            {
                return false;
            }

            return _traits.TryGetValue(traitId, out trait);
        }

        // Pairs each known trait in the set with its tier, in the set's ordinal order so hooks always run in the same order
        public IEnumerable<(Trait Trait, int Tier)> Resolve(TraitSet traits)
        {
            if (traits is null)
            {
                yield break;
            }

            foreach (KeyValuePair<string, int> entry in traits.Entries)
            {
                if (TryGetTrait(entry.Key, out Trait trait))
                {
                    yield return (trait, entry.Value);
                }
            }
        }

        public double GetPriceMultiplier(TraitSet traits)
        {
            double multiplier = 1;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                multiplier = trait.ModifySellPrice(multiplier, tier);
            }

            return Math.Max(multiplier, MIN_PRICE_MULTIPLIER);
        }

        public Trait RollTrait(Random random, IReadOnlyDictionary<string, int> weightBonuses)
        {
            List<(Trait Trait, int Weight)> weightedTraits = new();
            int totalWeight = 0;

            foreach (Trait trait in _rollableTraits)
            {
                int bonus = weightBonuses is null ? 0 : weightBonuses.GetValueOrDefault(trait.Id);
                int weight = trait.Weight + bonus;
                if (weight <= 0)
                {
                    continue;
                }

                weightedTraits.Add((trait, weight));
                totalWeight += weight;
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = random.Next(totalWeight);
            foreach ((Trait trait, int weight) in weightedTraits)
            {
                roll -= weight;
                if (roll < 0)
                {
                    return trait;
                }
            }

            return weightedTraits[^1].Trait;
        }

        // A conflict declared on either trait counts (both directions)
        public bool HasConflict(Trait incomingTrait, string existingId)
        {
            if (incomingTrait is null || incomingTrait.Id == existingId)
            {
                return false;
            }

            if (incomingTrait.ConflictsWith.Contains(existingId))
            {
                return true;
            }

            return TryGetTrait(existingId, out Trait existingTrait) && existingTrait.ConflictsWith.Contains(incomingTrait.Id);
        }

        public bool HasConflictWithAny(string incomingId, IEnumerable<string> existingIds)
        {
            if (TryGetTrait(incomingId, out Trait incomingTrait) is false)
            {
                return false;
            }

            return existingIds.Any(existingId => HasConflict(incomingTrait, existingId));
        }

        public bool HasExclusiveTrait(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.IsExclusive);
        }

        public bool HasPropagationBlockingTrait(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.BlocksPropagation);
        }

        public bool HasMutationReplacementBlocker(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.BlocksMutationReplacement);
        }

        public bool IsPriceTrait(string traitId)
        {
            return TryGetTrait(traitId, out Trait trait) && trait.IsPriceTrait;
        }

        public string GetTraitName(string traitId)
        {
            return TranslationHelper.GetOrDefault($"{GetTranslationPrefix(traitId)}.name", traitId);
        }

        public string GetTraitDescription(string traitId)
        {
            return TranslationHelper.GetOrDefault($"{GetTranslationPrefix(traitId)}.description", string.Empty);
        }

        private static string GetTranslationPrefix(string traitId)
        {
            return $"trait.{traitId?.ToLowerInvariant()}";
        }

        public string GetTierLabel(string traitId, int tier)
        {
            string name = GetTraitName(traitId);
            if (TryGetTrait(traitId, out Trait trait) && trait.MaxTier <= 1)
            {
                return name;
            }

            return $"{name} {ToRoman(tier)}";
        }

        public string GetLabel(string traitKey)
        {
            if (string.IsNullOrEmpty(traitKey))
            {
                return string.Empty;
            }

            return string.Join(", ", TraitSet.FromKey(traitKey).Entries
                .OrderBy(trait => GetTraitName(trait.Key), StringComparer.CurrentCulture)
                .Select(trait => GetTierLabel(trait.Key, trait.Value)));
        }

        private static string ToRoman(int value)
        {
            if (value >= 1 && value <= ROMAN_NUMERALS.Length)
            {
                return ROMAN_NUMERALS[value - 1];
            }

            return value.ToString();
        }

        // Finds every concrete Trait subclass in this assembly, so a new trait only needs its class
        private Dictionary<string, Trait> DiscoverTraits()
        {
            Dictionary<string, Trait> traits = new();

            // Sorted by type name so duplicate IDs always resolve the same way
            IEnumerable<Type> traitTypes = typeof(Trait).Assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(Trait)) && type.IsAbstract is false)
                .OrderBy(type => type.FullName, StringComparer.Ordinal);

            foreach (Type traitType in traitTypes)
            {
                if (traitType.GetConstructor(Type.EmptyTypes) is null)
                {
                    monitor.Log($"Skipped trait {traitType.FullName} since it has no parameterless constructor", LogLevel.Warn);
                    continue;
                }

                Trait trait = (Trait)Activator.CreateInstance(traitType);
                if (string.IsNullOrEmpty(trait.Id) || traits.ContainsKey(trait.Id))
                {
                    monitor.Log($"Skipped trait {traitType.FullName} since its ID is empty or already used by another trait", LogLevel.Warn);
                    continue;
                }

                traits[trait.Id] = trait;
            }

            return traits;
        }

        // TODO: Define combination traits
        private static List<CombinationData> GetDefaultCombinations()
        {
            return new List<CombinationData>();
        }
    }
}
