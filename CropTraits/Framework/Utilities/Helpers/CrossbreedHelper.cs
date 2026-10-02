using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Traits;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class CrossbreedHelper
    {
        public const double BASE_TIER_UPGRADE_CHANCE = 0.01;
        public const double TIER_UPGRADE_QUALITY_MULTIPLIER = 2.0;
        public const double HIGHER_TIER_CHANCE = 0.35;
        public const double UNIQUE_INHERIT_CHANCE = 0.5;

        // Expects parents in canonical order so random draws line up with the same parent every time
        public static CrossbreedResult Crossbreed(BlendParent firstParent, BlendParent secondParent, IReadOnlyList<CombinationData> combinations, Random random, Random upgradeRandom)
        {
            TraitSet firstRemaining = firstParent.Traits.Clone();
            TraitSet secondRemaining = secondParent.Traits.Clone();
            TraitSet resultTraits = new TraitSet();

            List<string> fusedCombinationKeys = ApplyCombinations(firstRemaining, secondRemaining, resultTraits, combinations, random);
            ApplySharedTraits(firstRemaining, secondRemaining, resultTraits, random);
            ApplyUniqueTraits(firstRemaining, secondRemaining, resultTraits, firstParent.Quality, secondParent.Quality, random);

            // Crossbreeds that roll nothing become Seedless
            if (resultTraits.Count == 0)
            {
                resultTraits.Set(SeedlessTrait.ID, 1);
            }

            TraitSet upgradedTraits = ApplyTierUpgrades(resultTraits, firstParent, secondParent, upgradeRandom);

            return new CrossbreedResult(resultTraits, upgradedTraits, fusedCombinationKeys);
        }

        public static List<string> ApplyCombinations(TraitSet firstRemaining, TraitSet secondRemaining, TraitSet resultTraits, IReadOnlyList<CombinationData> combinations, Random random)
        {
            List<string> fusedCombinationKeys = new();

            // Sorting before shuffling keeps results stable even if the definition order changes
            List<CombinationData> eligibleCombinations = combinations
                .Where(combination => IsSplitAcrossParents(combination, firstRemaining, secondRemaining))
                .OrderBy(combination => combination.Key, StringComparer.Ordinal)
                .ToList()
                .OrderBy(combination => random.Next())
                .ToList();

            foreach (CombinationData combination in eligibleCombinations)
            {
                if (resultTraits.Count >= TraitManager.MAX_TRAITS)
                {
                    break;
                }

                // An earlier fusion may have consumed one of this combination's traits
                if (IsSplitAcrossParents(combination, firstRemaining, secondRemaining) is false || random.NextDouble() >= combination.Chance)
                {
                    continue;
                }

                RemoveFromBoth(combination.FirstTraitId, firstRemaining, secondRemaining);
                RemoveFromBoth(combination.SecondTraitId, firstRemaining, secondRemaining);
                resultTraits.Set(combination.ResultTraitId, 1);
                fusedCombinationKeys.Add(combination.Key);
            }

            return fusedCombinationKeys;
        }

        public static void ApplySharedTraits(TraitSet firstRemaining, TraitSet secondRemaining, TraitSet resultTraits, Random random)
        {
            // Materialized before removing anything, and already in ordinal order
            List<string> sharedIds = firstRemaining.Ids
                .Where(secondRemaining.Has)
                .ToList();

            foreach (string traitId in sharedIds)
            {
                int firstTier = firstRemaining.GetTier(traitId);
                int secondTier = secondRemaining.GetTier(traitId);
                RemoveFromBoth(traitId, firstRemaining, secondRemaining);

                if (resultTraits.Count >= TraitManager.MAX_TRAITS || CropTraits.traitManager.HasConflictWithAny(traitId, resultTraits.Ids))
                {
                    continue;
                }

                // Matching tiers carry over as is, since their upgrade chance is rolled separately in ApplyTierUpgrades
                int resultTier;
                if (firstTier == secondTier)
                {
                    resultTier = firstTier;
                }
                else
                {
                    // Mismatched tiers lean toward the lower tier to reward matching parents
                    resultTier = random.NextDouble() < HIGHER_TIER_CHANCE ? Math.Max(firstTier, secondTier) : Math.Min(firstTier, secondTier);
                }

                resultTraits.Set(traitId, Math.Max(resultTier, resultTraits.GetTier(traitId)));
            }
        }

        /// <summary>
        /// Rolls the rare tier upgrade for traits both parents carry at the same tier as the fixed result.
        /// This uses true random rather than the pair seed, so the chance is a real rate per splice instead of a fixed yes or no for each pair.
        /// The fixed traits are left untouched, so the tracking keeps note of the pair's fixed result.
        /// </summary>
        public static TraitSet ApplyTierUpgrades(TraitSet fixedTraits, BlendParent firstParent, BlendParent secondParent, Random upgradeRandom)
        {
            TraitSet upgradedTraits = fixedTraits.Clone();
            double upgradeChance = GetTierUpgradeChance(firstParent.Quality, secondParent.Quality);

            foreach (KeyValuePair<string, int> trait in fixedTraits.Entries)
            {
                // Mismatched tiers and fused or unique traits never climb
                if (firstParent.Traits.GetTier(trait.Key) != trait.Value || secondParent.Traits.GetTier(trait.Key) != trait.Value)
                {
                    continue;
                }

                int maxTier = CropTraits.traitManager.TryGetTrait(trait.Key, out Trait definition) ? definition.MaxTier : 1;
                if (trait.Value >= maxTier)
                {
                    continue;
                }

                if (upgradeRandom.NextDouble() < upgradeChance)
                {
                    upgradedTraits.Set(trait.Key, trait.Value + 1);
                }
            }

            return upgradedTraits;
        }

        // Doubles for each quality step, using the average step of both parents (1% normal, 2% silver, 4% gold, 8% iridium)
        public static double GetTierUpgradeChance(int firstQuality, int secondQuality)
        {
            double averageQualityStep = (QualityHelper.GetQualityStep(firstQuality) + QualityHelper.GetQualityStep(secondQuality)) / 2.0;

            return BASE_TIER_UPGRADE_CHANCE * Math.Pow(TIER_UPGRADE_QUALITY_MULTIPLIER, averageQualityStep);
        }

        public static void ApplyUniqueTraits(TraitSet firstRemaining, TraitSet secondRemaining, TraitSet resultTraits, int firstQuality, int secondQuality, Random random)
        {
            List<KeyValuePair<string, int>> candidates = GetSurvivingTraits(firstRemaining, firstQuality, random)
                .Concat(GetSurvivingTraits(secondRemaining, secondQuality, random))
                .OrderBy(trait => random.Next())
                .ToList();

            // First placed wins, so a later conflicting trait is skipped
            foreach (KeyValuePair<string, int> candidate in candidates)
            {
                if (resultTraits.Count >= TraitManager.MAX_TRAITS)
                {
                    return;
                }

                if (resultTraits.Has(candidate.Key) || CropTraits.traitManager.HasConflictWithAny(candidate.Key, resultTraits.Ids))
                {
                    continue;
                }

                resultTraits.Set(candidate.Key, candidate.Value);
            }
        }

        // ToList forces each parent's rolls to happen in full before the next parent's
        public static List<KeyValuePair<string, int>> GetSurvivingTraits(TraitSet remainingTraits, int quality, Random random)
        {
            double inheritChance = Math.Min(UNIQUE_INHERIT_CHANCE + QualityHelper.GetBonus(quality), 1.0);

            return remainingTraits.Entries
                .Where(trait => random.NextDouble() < inheritChance)
                .ToList();
        }

        // Each source trait must be unique to its parent, so a parent crossed with a copy of itself can't fuse its own traits
        public static bool IsSplitAcrossParents(CombinationData combination, TraitSet firstRemaining, TraitSet secondRemaining)
        {
            bool isForwardSplit = IsUniqueTo(combination.FirstTraitId, firstRemaining, secondRemaining) && IsUniqueTo(combination.SecondTraitId, secondRemaining, firstRemaining);
            bool isReverseSplit = IsUniqueTo(combination.SecondTraitId, firstRemaining, secondRemaining) && IsUniqueTo(combination.FirstTraitId, secondRemaining, firstRemaining);

            return isForwardSplit || isReverseSplit;
        }

        public static bool IsUniqueTo(string traitId, TraitSet ownerTraits, TraitSet otherTraits)
        {
            return ownerTraits.Has(traitId) && otherTraits.Has(traitId) is false;
        }

        public static void RemoveFromBoth(string traitId, TraitSet firstRemaining, TraitSet secondRemaining)
        {
            firstRemaining.Remove(traitId);
            secondRemaining.Remove(traitId);
        }
    }
}
