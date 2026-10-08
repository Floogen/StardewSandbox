using CropTraits.Framework.Models;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Greedy and Swift, slowing its neighbors like Greedy III and feeding on the growth they lose
    public class ParasiticTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Parasitic";

        public const double SLOW_CHANCE = 0.30;

        // Matches Swift, since Parasitic replaces it
        private static readonly string[] CONFLICTS = { SluggishTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => GreedyTrait.ID;
        public override string SecondTraitId => SwiftTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (GreedyTrait.SlowNeighbors(context, SLOW_CHANCE) > 0)
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Each adjacent crop of another type has a {ToPercent(SLOW_CHANCE)} chance to skip growing each night. This crop grows an extra day on any night it slows one";
        }

        public override string GetNotes()
        {
            return "The extra day only counts on nights this crop grows (when it's watered). Crops with Balanced aren't affected.";
        }
    }
}
