using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley.GameData.Crops;
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
            _combinations = BuildCombinations();

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

        // Artisan goods also get each trait's artisan only adjustment
        public double GetPriceMultiplier(TraitSet traits, bool isArtisanGood)
        {
            double multiplier = 1;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                multiplier = trait.ModifySellPrice(multiplier, tier);
                if (isArtisanGood)
                {
                    multiplier = trait.ModifyArtisanSellPrice(multiplier, tier);
                }
            }

            return Math.Max(multiplier, MIN_PRICE_MULTIPLIER);
        }

        public float GetProduceScale(TraitSet traits)
        {
            float scale = 1f;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                scale = trait.ModifyProduceScale(scale, tier);
            }

            return scale;
        }

        // Traits that can't apply to the crop are left out entirely, so they never take a share of the roll
        public Trait RollTrait(Random random, IReadOnlyDictionary<string, int> weightBonuses, CropData cropData)
        {
            List<(Trait Trait, int Weight)> weightedTraits = new();
            int totalWeight = 0;

            foreach (Trait trait in _rollableTraits)
            {
                if (trait.CanApplyTo(cropData) is false)
                {
                    continue;
                }

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

            // Exclusive traits never share a crop, so they conflict with everything
            if (incomingTrait.IsExclusive || incomingTrait.ConflictsWith.Contains(existingId))
            {
                return true;
            }

            return TryGetTrait(existingId, out Trait existingTrait) && (existingTrait.IsExclusive || existingTrait.ConflictsWith.Contains(incomingTrait.Id));
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

        public bool IgnoresFertilizer(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.IgnoresFertilizer);
        }

        public bool IgnoresGrowthModifiers(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.IgnoresGrowthModifiers);
        }

        public bool WithersWhenLeftReady(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.WithersWhenLeftReady);
        }

        public int GetOutOfSeasonNights(TraitSet traits)
        {
            int nights = 0;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                nights = Math.Max(nights, trait.GetOutOfSeasonNights(tier));
            }

            return nights;
        }

        // A combination trait in the set already holds this trait, so the trait can't rejoin the set
        public bool IsAbsorbedBy(string traitId, TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait is CombinationTrait combinationTrait && combinationTrait.IsComposedOf(traitId));
        }

        // Removes the traits a combination trait is made from, returning true if any were removed
        public bool RemoveAbsorbedTraits(string traitId, TraitSet traits)
        {
            if (TryGetTrait(traitId, out Trait trait) is false || trait is not CombinationTrait combinationTrait)
            {
                return false;
            }

            bool isFirstRemoved = traits.Remove(combinationTrait.FirstTraitId);
            bool isSecondRemoved = traits.Remove(combinationTrait.SecondTraitId);

            return isFirstRemoved || isSecondRemoved;
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

        public static string ToRoman(int value)
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

        // Each combination trait declares its own pair, so a new combination only needs its class
        private List<CombinationData> BuildCombinations()
        {
            List<CombinationData> combinations = new();

            IEnumerable<CombinationTrait> combinationTraits = _traits.Values
                .OfType<CombinationTrait>()
                .OrderBy(trait => trait.Id, StringComparer.Ordinal);

            foreach (CombinationTrait combinationTrait in combinationTraits)
            {
                if (IsValidCombination(combinationTrait) is false)
                {
                    monitor.Log($"Skipped combination trait {combinationTrait.Id} since its pair ({combinationTrait.FirstTraitId} + {combinationTrait.SecondTraitId}) is missing, repeated or includes itself", LogLevel.Warn);
                    continue;
                }

                combinations.Add(new CombinationData { FirstTraitId = combinationTrait.FirstTraitId, SecondTraitId = combinationTrait.SecondTraitId, ResultTraitId = combinationTrait.Id, Chance = combinationTrait.FusionChance });
            }

            return combinations;
        }

        private bool IsValidCombination(CombinationTrait combinationTrait)
        {
            string firstTraitId = combinationTrait.FirstTraitId;
            string secondTraitId = combinationTrait.SecondTraitId;
            if (_traits.ContainsKey(firstTraitId ?? string.Empty) is false || _traits.ContainsKey(secondTraitId ?? string.Empty) is false)
            {
                return false;
            }

            return firstTraitId != secondTraitId && combinationTrait.IsComposedOf(combinationTrait.Id) is false;
        }
    }
}
