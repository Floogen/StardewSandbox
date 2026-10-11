using CropTraits.Framework.Models;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Greedy and Swift, slowing its neighbors like Greedy III and feeding on the growth they lose
    public class ParasiticTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Parasitic";

        public const double SLOW_CHANCE = 0.30;
        public const double FEED_CHANCE_PER_SLOWED_NEIGHBOR = 0.25;

        // Matches Swift, since Parasitic replaces it
        private static readonly string[] CONFLICTS = { SluggishTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => GreedyTrait.ID;
        public override string SecondTraitId => SwiftTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            int slowedCount = GreedyTrait.SlowNeighbors(context, SLOW_CHANCE);
            if (slowedCount > 0 && context.Random.NextDouble() < GetFeedChance(slowedCount))
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        // Each slowed neighbor is its own chance, so surrounding it with other crops never makes the extra day certain
        public static double GetFeedChance(int slowedCount)
        {
            return 1 - Math.Pow(1 - FEED_CHANCE_PER_SLOWED_NEIGHBOR, slowedCount);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { slowChance = ToPercentValue(SLOW_CHANCE), feedChance = ToPercentValue(FEED_CHANCE_PER_SLOWED_NEIGHBOR) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
