using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Managers
{
    public class TraitManager : BaseManager
    {
        public const int MAX_TRAITS = 3;

        // Mutation picks a polarity with these shares first, so the balance holds however many traits each polarity has
        public const double POSITIVE_SHARE = 0.45;
        public const double NEUTRAL_SHARE = 0.15;
        public const double NEGATIVE_SHARE = 0.40;

        // Polarities are always rolled in this order, so the roll is deterministic for a given seed
        private static readonly TraitPolarity[] POLARITY_ORDER = { TraitPolarity.Positive, TraitPolarity.Neutral, TraitPolarity.Negative };
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
        /// <summary>
        /// Picks a polarity using its fixed share, then a trait of that polarity by weight.
        /// Neighbor bonuses raise a trait's weight within its polarity and scale that polarity's share by the same proportion, so they pull on both rolls.
        /// A polarity with no traits the crop can roll is left out and the other shares fill its place.
        /// </summary>
        public Trait RollTrait(Random random, IReadOnlyDictionary<string, int> weightBonuses, CropData cropData)
        {
            Dictionary<TraitPolarity, List<(Trait Trait, int Weight)>> weightedTraitsByPolarity = new();
            Dictionary<TraitPolarity, int> baseWeightsByPolarity = new();

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

                if (weightedTraitsByPolarity.TryGetValue(trait.Polarity, out List<(Trait Trait, int Weight)> weightedTraits) is false)
                {
                    weightedTraits = new();
                    weightedTraitsByPolarity[trait.Polarity] = weightedTraits;
                }

                weightedTraits.Add((trait, weight));
                baseWeightsByPolarity[trait.Polarity] = baseWeightsByPolarity.GetValueOrDefault(trait.Polarity) + Math.Max(trait.Weight, 0);
            }

            List<(TraitPolarity Polarity, double Share)> polarityShares = new();
            foreach (TraitPolarity polarity in POLARITY_ORDER)
            {
                if (weightedTraitsByPolarity.TryGetValue(polarity, out List<(Trait Trait, int Weight)> weightedTraits) is false)
                {
                    continue;
                }

                // A polarity whose traits only have weight from bonuses still gets its base share
                int totalWeight = weightedTraits.Sum(entry => entry.Weight);
                int baseWeight = baseWeightsByPolarity.GetValueOrDefault(polarity);
                double bonusScale = baseWeight > 0 ? (double)totalWeight / baseWeight : 1;

                polarityShares.Add((polarity, GetPolarityShare(polarity) * bonusScale));
            }

            if (polarityShares.Count == 0)
            {
                return null;
            }

            double polarityRoll = random.NextDouble() * polarityShares.Sum(entry => entry.Share);
            TraitPolarity rolledPolarity = polarityShares[^1].Polarity;
            foreach ((TraitPolarity polarity, double share) in polarityShares)
            {
                polarityRoll -= share;
                if (polarityRoll < 0)
                {
                    rolledPolarity = polarity;
                    break;
                }
            }

            return PickWeightedTrait(weightedTraitsByPolarity[rolledPolarity], random);
        }

        public static double GetPolarityShare(TraitPolarity polarity)
        {
            return polarity switch
            {
                TraitPolarity.Neutral => NEUTRAL_SHARE,
                TraitPolarity.Negative => NEGATIVE_SHARE,
                _ => POSITIVE_SHARE
            };
        }

        // The traits are in ordinal order, so the pick only depends on the roll
        public static Trait PickWeightedTrait(List<(Trait Trait, int Weight)> weightedTraits, Random random)
        {
            int roll = random.Next(weightedTraits.Sum(entry => entry.Weight));
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

        public bool IsCombinationTrait(string traitId)
        {
            return TryGetTrait(traitId, out Trait trait) && trait is CombinationTrait;
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

        public bool GlowsAtNight(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.GlowsAtNight);
        }

        public bool GuaranteesDilutionSplit(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.GuaranteesDilutionSplit);
        }

        public bool AlwaysPassesOnTraits(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.AlwaysPassesOnTraits);
        }

        public bool IsRemovedAfterHarvest(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.IsRemovedAfterHarvest);
        }

        public bool LosesQualityWhenLeftReady(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.LosesQualityWhenLeftReady);
        }

        public bool ClearsNearbyWeeds(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.ClearsNearbyWeeds);
        }

        public bool IsConductive(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.IsConductive);
        }

        public double GetGiftFriendshipMultiplier(TraitSet traits)
        {
            double multiplier = 1;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                multiplier = trait.ModifyGiftFriendship(multiplier, tier);
            }

            return Math.Max(multiplier, 0);
        }

        public int GetExtraSeeds(TraitSet traits, Random random)
        {
            int extraSeeds = 0;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                extraSeeds += trait.GetExtraSeeds(random, tier);
            }

            return Math.Max(extraSeeds, 0);
        }

        public int GetNeighborInfluenceMultiplier(TraitSet traits)
        {
            int multiplier = 1;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                multiplier = Math.Max(multiplier, trait.GetNeighborInfluenceMultiplier(tier));
            }

            return multiplier;
        }

        /// <summary>
        /// Swaps each trait like Latent for a random positive trait the crop can roll, picked by weight from the traits it doesn't already have and wouldn't conflict with.
        /// A trait with nothing left to awaken into stays as it is. Returns the same set when nothing awakens, otherwise a new one.
        /// </summary>
        public TraitSet AwakenTraits(TraitSet traits, CropData cropData, Random random)
        {
            List<(Trait Trait, int Tier)> awakeningTraits = Resolve(traits)
                .Where(entry => entry.Trait.AwakensWhenSpliced)
                .ToList();

            if (awakeningTraits.Count == 0)
            {
                return traits;
            }

            TraitSet awakenedTraits = traits.Clone();
            foreach ((Trait awakeningTrait, int tier) in awakeningTraits)
            {
                awakenedTraits.Remove(awakeningTrait.Id);

                Trait awakenedTrait = RollAwakenedTrait(awakenedTraits, cropData, random);
                if (awakenedTrait is null)
                {
                    awakenedTraits.Set(awakeningTrait.Id, tier);
                    continue;
                }

                awakenedTraits.Set(awakenedTrait.Id, 1);
            }

            return awakenedTraits;
        }

        // The rollable traits are in ordinal order, so the pick only depends on the roll
        public Trait RollAwakenedTrait(TraitSet existingTraits, CropData cropData, Random random)
        {
            List<(Trait Trait, int Weight)> candidates = _rollableTraits
                .Where(trait => trait.Polarity == TraitPolarity.Positive && trait.Weight > 0 && existingTraits.Has(trait.Id) is false && trait.CanApplyTo(cropData))
                .Where(trait => HasConflictWithAny(trait.Id, existingTraits.Ids) is false)
                .Select(trait => (trait, trait.Weight))
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            return PickWeightedTrait(candidates, random);
        }

        public Color GetTint(TraitSet traits, string seedId)
        {
            Color tint = Color.White;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                tint = trait.ModifyTint(tint, seedId, tier);
            }

            return tint;
        }

        public double GetGiantCropBonusChance(TraitSet traits)
        {
            double chance = 0;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                chance = trait.ModifyGiantCropChance(chance, tier);
            }

            return Math.Clamp(chance, 0, 1);
        }

        public double GetGiantCropYieldMultiplier(TraitSet traits)
        {
            double multiplier = 1;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                multiplier = trait.ModifyGiantCropYield(multiplier, tier);
            }

            return Math.Max(multiplier, 0);
        }

        public bool IsIgnoredByCrows(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.IsIgnoredByCrows);
        }

        public bool ProtectsNeighborsFromCrows(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.ProtectsNeighborsFromCrows);
        }

        public bool IgnoresScarecrows(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.IgnoresScarecrows);
        }

        public double GetFoodRestorationMultiplier(TraitSet traits)
        {
            double multiplier = 1;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                multiplier = trait.ModifyFoodRestoration(multiplier, tier);
            }

            return Math.Max(multiplier, 0);
        }

        public List<Buff> GetFoodBuffs(TraitSet traits)
        {
            List<Buff> buffs = new();
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                Buff buff = trait.GetFoodBuff(tier);
                if (buff is not null)
                {
                    buffs.Add(buff);
                }
            }

            return buffs;
        }

        public bool GrowsInEverySeason(TraitSet traits)
        {
            return Resolve(traits).Any(entry => entry.Trait.GrowsInEverySeason);
        }

        public int GetExtraRegrowths(TraitSet traits)
        {
            int regrowths = 0;
            foreach ((Trait trait, int tier) in Resolve(traits))
            {
                regrowths = Math.Max(regrowths, trait.GetExtraRegrowths(tier));
            }

            return regrowths;
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
