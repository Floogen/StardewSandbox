using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Traits;
using StardewValley.GameData.Crops;
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
        public const double QUALITY_LEAD_FAVOR_PER_STEP = 0.05;

        // Expects parents in canonical order so random draws line up with the same parent whichever slot it was in
        public static CrossbreedResult Crossbreed(BlendParent firstParent, BlendParent secondParent, IReadOnlyList<CombinationData> combinations, CropData cropData, Random random)
        {
            TraitSet firstRemaining = firstParent.Traits.Clone();
            TraitSet secondRemaining = secondParent.Traits.Clone();
            TraitSet resultTraits = new TraitSet();

            List<string> fusedCombinationKeys = ApplyCombinations(firstRemaining, secondRemaining, resultTraits, combinations, random);
            ApplySharedTraits(firstRemaining, secondRemaining, resultTraits, firstParent.Quality, secondParent.Quality, random);
            bool isFirstAlwaysInherited = CropTraits.traitManager.AlwaysPassesOnTraits(firstParent.Traits);
            bool isSecondAlwaysInherited = CropTraits.traitManager.AlwaysPassesOnTraits(secondParent.Traits);

            // Read from each parent's full traits, so a trait like Recessive still counts after a fusion consumed it
            double firstInheritMultiplier = CropTraits.traitManager.GetInheritChanceMultiplier(firstParent.Traits);
            double secondInheritMultiplier = CropTraits.traitManager.GetInheritChanceMultiplier(secondParent.Traits);
            ApplyUniqueTraits(firstRemaining, secondRemaining, resultTraits, firstParent.Quality, secondParent.Quality, isFirstAlwaysInherited, isSecondAlwaysInherited, firstInheritMultiplier, secondInheritMultiplier, random);

            // Traits like Latent awaken before tier upgrades, so the new trait is treated like any other result
            resultTraits = CropTraits.traitManager.AwakenTraits(resultTraits, cropData, random);

            // Crossbreeds that roll nothing become Seedless
            if (resultTraits.Count == 0)
            {
                resultTraits.Set(SeedlessTrait.ID, 1);
            }

            TraitSet upgradedTraits = ApplyTierUpgrades(resultTraits, firstParent, secondParent, random);

            return new CrossbreedResult(resultTraits, upgradedTraits, fusedCombinationKeys);
        }

        public static List<string> ApplyCombinations(TraitSet firstRemaining, TraitSet secondRemaining, TraitSet resultTraits, IReadOnlyList<CombinationData> combinations, Random random)
        {
            List<string> fusedCombinationKeys = new();

            // Sorting before shuffling keeps results stable even if the definition order changes
            List<CombinationData> eligibleCombinations = combinations
                .Where(combination => IsSplitAcrossParents(combination, firstRemaining, secondRemaining) && IsCarriedByEitherParent(combination.ResultTraitId, firstRemaining, secondRemaining) is false)
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
                if (IsSplitAcrossParents(combination, firstRemaining, secondRemaining) is false)
                {
                    continue;
                }

                // A result that conflicts with an earlier fusion is skipped without a roll, leaving its source traits to inherit normally
                if (CropTraits.traitManager.HasConflictWithAny(combination.ResultTraitId, resultTraits.Ids) || random.NextDouble() >= combination.Chance)
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

        public static void ApplySharedTraits(TraitSet firstRemaining, TraitSet secondRemaining, TraitSet resultTraits, int firstQuality, int secondQuality, Random random)
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

                if (CanPlaceTrait(traitId, resultTraits) is false)
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
                    // Mismatched tiers lean toward the lower tier to reward matching parents, shifted toward the higher quality parent's tier
                    int higherTierQuality = firstTier > secondTier ? firstQuality : secondQuality;
                    int lowerTierQuality = firstTier > secondTier ? secondQuality : firstQuality;
                    resultTier = random.NextDouble() < GetHigherTierChance(higherTierQuality, lowerTierQuality) ? Math.Max(firstTier, secondTier) : Math.Min(firstTier, secondTier);
                }

                resultTraits.Set(traitId, resultTier);
            }
        }

        /// <summary>
        /// Rolls the rare tier upgrade for traits both parents carry at the same tier as the base result.
        /// The base traits are left untouched, so the Seed Splicer can tell which traits grew stronger.
        /// </summary>
        public static TraitSet ApplyTierUpgrades(TraitSet baseTraits, BlendParent firstParent, BlendParent secondParent, Random random)
        {
            TraitSet upgradedTraits = baseTraits.Clone();
            double upgradeChance = GetTierUpgradeChance(firstParent.Quality, secondParent.Quality);

            foreach (KeyValuePair<string, int> trait in baseTraits.Entries)
            {
                // Mismatched tiers and fused or unique traits never climb
                if (firstParent.Traits.GetTier(trait.Key) != trait.Value || secondParent.Traits.GetTier(trait.Key) != trait.Value)
                {
                    continue;
                }

                if (trait.Value >= CropTraits.traitManager.GetMaxTier(trait.Key))
                {
                    continue;
                }

                if (random.NextDouble() < upgradeChance)
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

        // Each quality step the higher tier's parent is ahead raises the chance while each step behind lowers it (35% when qualities match)
        public static double GetHigherTierChance(int higherTierQuality, int lowerTierQuality)
        {
            double qualityShift = (QualityHelper.GetQualityStep(higherTierQuality) - QualityHelper.GetQualityStep(lowerTierQuality)) * QUALITY_LEAD_FAVOR_PER_STEP;

            return Math.Clamp(HIGHER_TIER_CHANCE + qualityShift, 0, 1);
        }

        public static void ApplyUniqueTraits(TraitSet firstRemaining, TraitSet secondRemaining, TraitSet resultTraits, int firstQuality, int secondQuality, bool isFirstAlwaysInherited, bool isSecondAlwaysInherited, double firstInheritMultiplier, double secondInheritMultiplier, Random random)
        {
            List<KeyValuePair<string, int>> firstSurviving = GetSurvivingTraits(firstRemaining, firstQuality, isFirstAlwaysInherited, firstInheritMultiplier, random);
            List<KeyValuePair<string, int>> secondSurviving = GetSurvivingTraits(secondRemaining, secondQuality, isSecondAlwaysInherited, secondInheritMultiplier, random);
            double firstPriority = QualityHelper.GetQualityLead(firstQuality, secondQuality) * QUALITY_LEAD_FAVOR_PER_STEP;
            double secondPriority = QualityHelper.GetQualityLead(secondQuality, firstQuality) * QUALITY_LEAD_FAVOR_PER_STEP;

            // The higher quality parent's traits tend to be placed earlier, so they win more conflicts and open slots
            List<KeyValuePair<string, int>> candidates = firstSurviving
                .Select(trait => (Trait: trait, SortKey: random.NextDouble() - firstPriority))
                .Concat(secondSurviving.Select(trait => (Trait: trait, SortKey: random.NextDouble() - secondPriority)))
                .OrderBy(candidate => candidate.SortKey)
                .Select(candidate => candidate.Trait)
                .ToList();

            // First placed wins, so a later conflicting trait is skipped
            foreach (KeyValuePair<string, int> candidate in candidates)
            {
                if (CanPlaceTrait(candidate.Key, resultTraits) is false)
                {
                    continue;
                }

                resultTraits.Set(candidate.Key, candidate.Value);
            }
        }

        // A combination trait and the traits it was made from can share a crop, so only the cap and conflicts decide
        public static bool CanPlaceTrait(string traitId, TraitSet resultTraits)
        {
            return resultTraits.Count < TraitManager.MAX_TRAITS && CropTraits.traitManager.HasConflictWithAny(traitId, resultTraits.Ids) is false;
        }

        // ToList forces each parent's rolls to happen in full before the next parent's. The roll comes first so traits like Heirloom keep the draws the same
        // Traits like Recessive scale the chance after the quality bonus, while the roll still happens for every trait so the draws stay the same
        public static List<KeyValuePair<string, int>> GetSurvivingTraits(TraitSet remainingTraits, int quality, bool isAlwaysInherited, double inheritMultiplier, Random random)
        {
            double inheritChance = Math.Clamp(Math.Min(UNIQUE_INHERIT_CHANCE + QualityHelper.GetBonus(quality), 1.0) * inheritMultiplier, 0, 1);

            return remainingTraits.Entries
                .Where(trait => random.NextDouble() < inheritChance || isAlwaysInherited)
                .ToList();
        }

        // Each source trait must be unique to its parent, so a parent crossed with a copy of itself can't fuse its own traits
        public static bool IsSplitAcrossParents(CombinationData combination, TraitSet firstRemaining, TraitSet secondRemaining)
        {
            bool isForwardSplit = IsUniqueTo(combination.FirstTraitId, firstRemaining, secondRemaining) && IsUniqueTo(combination.SecondTraitId, secondRemaining, firstRemaining);
            bool isReverseSplit = IsUniqueTo(combination.SecondTraitId, firstRemaining, secondRemaining) && IsUniqueTo(combination.FirstTraitId, secondRemaining, firstRemaining);

            return isForwardSplit || isReverseSplit;
        }

        // A parent already carrying the result passes it on normally, so its parts aren't consumed to fuse it again
        public static bool IsCarriedByEitherParent(string traitId, TraitSet firstRemaining, TraitSet secondRemaining)
        {
            return firstRemaining.Has(traitId) || secondRemaining.Has(traitId);
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
