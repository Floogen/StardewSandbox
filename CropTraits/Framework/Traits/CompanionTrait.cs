using CropTraits.Framework.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Traits
{
    // Named after companion planting, where different crops are grown together to help each other
    public class CompanionTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Companion";

        public const double BOOST_CHANCE_PER_TYPE_PER_TIER = 0.04;
        public const int MAX_COUNTED_TYPES = 4;

        private static readonly string[] CONFLICTS = { SolitaryTrait.ID };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            int otherTypeCount = context.Neighbors
                .Select(neighbor => neighbor.State.CropId)
                .Where(cropId => cropId != context.State.CropId)
                .Distinct()
                .Count();

            if (otherTypeCount == 0)
            {
                return;
            }

            if (context.Random.NextDouble() < GetBoostChance(otherTypeCount, tier))
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        public static double GetBoostChance(int otherTypeCount, int tier)
        {
            return Math.Min(otherTypeCount, MAX_COUNTED_TYPES) * BOOST_CHANCE_PER_TYPE_PER_TIER * tier;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(BOOST_CHANCE_PER_TYPE_PER_TIER * tier)} chance to grow an extra day each night for every other crop type around it (up to {ToPercent(GetBoostChance(MAX_COUNTED_TYPES, tier))})";
        }

        public override string GetNotes()
        {
            return $"Covers all eight surrounding tiles. Each different crop type counts once, up to {MAX_COUNTED_TYPES} types. The extra day only counts on nights the crop grows (when it's watered).";
        }
    }
}
