using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Managers
{
    public class BlendManager : BaseManager
    {
        public const int REQUIRED_STACK_PER_SLOT = 5;
        public const int MIN_SEED_OUTPUT = 1;
        public const int MAX_SEED_OUTPUT = 3;

        // Reverse lookup from qualified harvest item ID to the seeds that grow it, rebuilt when Data/Crops changes
        private Dictionary<string, List<string>> _seedIdsByHarvestItem;

        /// <summary>Validates Seed Splicer inputs and handles the crossbreeding / dilution.</summary>
        public BlendManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public void InvalidateCropCache()
        {
            _seedIdsByHarvestItem = null;
        }

        public bool HasTraits(Item item)
        {
            return item is not null && TraitModDataHelper.HasTraits(item.modData);
        }

        public string GetSeedId(Item produce)
        {
            if (produce is null)
            {
                return null;
            }

            if (produce.modData.TryGetValue(CropTraits.SEED_ID_MOD_DATA_KEY, out string seedId) && string.IsNullOrEmpty(seedId) is false)
            {
                return seedId;
            }

            // Traited items without a seed ID (artisan goods) are never treated as produce
            if (HasTraits(produce))
            {
                return null;
            }

            return GetSeedIdByReverseLookup(produce);
        }

        public string GetSeedIdByReverseLookup(Item produce)
        {
            EnsureCropCache();

            // Only unambiguous matches count, since some crops share a harvest item
            if (_seedIdsByHarvestItem.TryGetValue(produce.QualifiedItemId, out List<string> seedIds) && seedIds.Count == 1)
            {
                return seedIds[0];
            }

            return null;
        }

        public bool IsPropagationBlocked(Item item)
        {
            return item is not null && CropTraits.traitManager.HasPropagationBlockingTrait(TraitModDataHelper.GetTraits(item.modData));
        }

        public bool HasEnoughForSlot(Item produce)
        {
            return produce is not null && produce.Stack >= REQUIRED_STACK_PER_SLOT;
        }

        // Whether a candidate can go into a slot, given whatever already sits in the other slot
        public bool CanPlace(Item candidate, Item otherParent)
        {
            // Stacks too small to splice are never eligible, so they aren't offered at all
            return HasEnoughForSlot(candidate) && CanPlaceIgnoringStack(candidate, otherParent);
        }

        public bool IsOnlyShortOfStack(Item candidate, Item otherParent)
        {
            return candidate is not null && HasEnoughForSlot(candidate) is false && CanPlaceIgnoringStack(candidate, otherParent);
        }

        public bool CanPlaceIgnoringStack(Item candidate, Item otherParent)
        {
            if (candidate is null || IsPropagationBlocked(candidate))
            {
                return false;
            }

            string candidateSeedId = GetSeedId(candidate);
            if (candidateSeedId is null)
            {
                return false;
            }

            if (otherParent is null)
            {
                return true;
            }

            // Two untraited parents have nothing to blend
            return GetSeedId(otherParent) == candidateSeedId && (HasTraits(candidate) || HasTraits(otherParent));
        }

        public bool HasEligibleItem(IEnumerable<Item> items, Item otherParent)
        {
            return items.Any(item => CanPlace(item, otherParent));
        }

        public bool CanBlend(Item firstParent, Item secondParent)
        {
            if (HasEnoughForSlot(firstParent) is false || HasEnoughForSlot(secondParent) is false)
            {
                return false;
            }

            // CanPlace only checks the candidate, so the other parent is checked for propagation blocking too
            if (CanPlace(firstParent, secondParent) is false || IsPropagationBlocked(secondParent))
            {
                return false;
            }

            // Two untraited parents have nothing to blend
            if (HasTraits(firstParent) is false && HasTraits(secondParent) is false)
            {
                return false;
            }

            return IsFullyMaxedIdenticalPair(firstParent, secondParent) is false;
        }

        // Both parents carry the same traits at the same tiers, so a tier upgrade is the only possible change
        public bool IsIdenticalPair(Item firstParent, Item secondParent)
        {
            if (HasTraits(firstParent) is false || HasTraits(secondParent) is false)
            {
                return false;
            }

            return TraitModDataHelper.GetTraitKey(firstParent.modData) == TraitModDataHelper.GetTraitKey(secondParent.modData);
        }

        // With every trait at max tier, an identical pair can't change at all, so splicing would only waste the crops
        public bool IsFullyMaxedIdenticalPair(Item firstParent, Item secondParent)
        {
            return IsIdenticalPair(firstParent, secondParent) && GetUpgradeableTraitCount(TraitModDataHelper.GetTraits(firstParent.modData)) == 0;
        }

        public int GetUpgradeableTraitCount(TraitSet traits)
        {
            return traits.Entries.Count(trait => trait.Value < CropTraits.traitManager.GetMaxTier(trait.Key));
        }

        // Each upgradeable trait rolls on its own, so this is the chance that at least one increases its tier
        public double GetAnyTierUpgradeChance(Item firstParent, Item secondParent)
        {
            return GetAnyTierUpgradeChance(firstParent, secondParent, GetUpgradeableTraitCount(TraitModDataHelper.GetTraits(firstParent.modData)));
        }

        public double GetAnyTierUpgradeChance(Item firstParent, Item secondParent, int upgradeableCount)
        {
            double upgradeChance = CrossbreedHelper.GetTierUpgradeChance(firstParent.Quality, secondParent.Quality);

            return 1 - Math.Pow(1 - upgradeChance, upgradeableCount);
        }

        public bool TryGetKnownResult(Item firstItem, Item secondItem, out TraitSet resultTraits)
        {
            resultTraits = null;
            if (HasTraits(firstItem) is false || HasTraits(secondItem) is false)
            {
                return false;
            }

            string seedId = GetSeedId(firstItem);
            if (seedId is null)
            {
                return false;
            }

            (BlendParent first, BlendParent second) = BlendRandomHelper.GetCanonicalOrder(ToBlendParent(firstItem), ToBlendParent(secondItem));
            string pairKey = BlendRandomHelper.BuildPairKey(seedId, first, second);
            if (CropTraits.discoveryManager.Data.BlendJournal.TryGetValue(pairKey, out BlendJournalEntryData entry) is false)
            {
                return false;
            }

            resultTraits = TraitSet.FromKey(entry.ResultTraitKey);
            return true;
        }

        // Matches ApplyTierUpgrades: only result traits both parents carry at that same tier, and below max tier, can climb
        public int GetUpgradeableResultTraitCount(TraitSet resultTraits, Item firstParent, Item secondParent)
        {
            TraitSet firstTraits = TraitModDataHelper.GetTraits(firstParent.modData);
            TraitSet secondTraits = TraitModDataHelper.GetTraits(secondParent.modData);

            return resultTraits.Entries.Count(trait => firstTraits.GetTier(trait.Key) == trait.Value && secondTraits.GetTier(trait.Key) == trait.Value && trait.Value < CropTraits.traitManager.GetMaxTier(trait.Key));
        }

        public BlendOutcome Blend(string seedId, Item firstItem, Item secondItem)
        {
            BlendParent firstParent = ToBlendParent(firstItem);
            BlendParent secondParent = ToBlendParent(secondItem);

            // Dilution is a gamble rather than a recipe, so it uses true random and isn't recorded in the journal
            if (firstParent.Traits.Count == 0 || secondParent.Traits.Count == 0)
            {
                BlendParent traitedParent = firstParent.Traits.Count == 0 ? secondParent : firstParent;
                TraitSet dilutedTraits = DilutionHelper.Dilute(traitedParent.Traits, traitedParent.Quality, Game1.random);

                return new BlendOutcome(CreateSeeds(seedId, dilutedTraits), new TraitSet());
            }

            (BlendParent first, BlendParent second) = BlendRandomHelper.GetCanonicalOrder(firstParent, secondParent);
            string pairKey = BlendRandomHelper.BuildPairKey(seedId, first, second);
            Random random = BlendRandomHelper.Create(pairKey);

            // Tier upgrades use the game's random so a rare chance is a real rate per splice
            CrossbreedResult result = CrossbreedHelper.Crossbreed(first, second, CropTraits.traitManager.Combinations, random, Game1.random);

            BlendJournalEntryData journalEntry = new BlendJournalEntryData
            {
                SeedId = seedId,
                FirstParentKey = first.SortKey,
                SecondParentKey = second.SortKey,
                // The fixed result is recorded, so the journal stays accurate for the pair even after a lucky upgrade
                ResultTraitKey = result.FixedTraits.ToKey(),
                FirstSplicedDay = Game1.Date.TotalDays
            };

            CropTraits.discoveryManager.RecordBlend(pairKey, journalEntry, result.FusedCombinationKeys);

            return new BlendOutcome(CreateSeeds(seedId, result.Traits), GetUpgradedTraits(result.FixedTraits, result.Traits));
        }

        // Seed count is truly random, independent of the trait outcome
        public StardewValley.Object CreateSeeds(string seedId, TraitSet traits)
        {
            int seedCount = Game1.random.Next(MIN_SEED_OUTPUT, MAX_SEED_OUTPUT + 1);

            StardewValley.Object seeds = ItemRegistry.Create<StardewValley.Object>(seedId, seedCount);
            TraitModDataHelper.SetTraits(seeds.modData, traits);

            return seeds;
        }

        public TraitSet GetUpgradedTraits(TraitSet fixedTraits, TraitSet resultTraits)
        {
            TraitSet upgradedTraits = new TraitSet();
            foreach (KeyValuePair<string, int> trait in resultTraits.Entries)
            {
                if (trait.Value > fixedTraits.GetTier(trait.Key))
                {
                    upgradedTraits.Set(trait.Key, trait.Value);
                }
            }

            return upgradedTraits;
        }

        private BlendParent ToBlendParent(Item produce)
        {
            return new BlendParent(TraitModDataHelper.GetTraits(produce.modData), produce.Quality);
        }

        private void EnsureCropCache()
        {
            if (_seedIdsByHarvestItem is not null)
            {
                return;
            }

            _seedIdsByHarvestItem = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, CropData> pair in Game1.cropData)
            {
                if (string.IsNullOrEmpty(pair.Value?.HarvestItemId))
                {
                    continue;
                }

                string qualifiedHarvestItemId = ItemRegistry.QualifyItemId(pair.Value.HarvestItemId);
                if (string.IsNullOrEmpty(qualifiedHarvestItemId))
                {
                    continue;
                }

                if (_seedIdsByHarvestItem.TryGetValue(qualifiedHarvestItemId, out List<string> seedIds) is false)
                {
                    seedIds = new List<string>();
                    _seedIdsByHarvestItem[qualifiedHarvestItemId] = seedIds;
                }

                seedIds.Add(pair.Key);
            }
        }
    }
}
