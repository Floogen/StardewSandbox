using CropTraits.Framework.Models;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Greedy, helping crops of other types grow
    public class NurturingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Nurturing";

        public const double BOOST_CHANCE_PER_TIER = 0.10;

        // Parasitic is included since it keeps Greedy's slowdown
        private static readonly string[] CONFLICTS = { GreedyTrait.ID, ParasiticTrait.ID };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            BoostNeighbors(context, BOOST_CHANCE_PER_TIER * tier);
        }

        // Rolls once for each adjacent crop of another type (in a fixed order), returning how many were boosted
        public static int BoostNeighbors(NightlyContext context, double chance)
        {
            int boostedCount = 0;
            foreach (NightlyNeighbor neighbor in context.Neighbors)
            {
                // A neighbor already boosted tonight is skipped, so surrounding a crop with Nurturing crops can't stack extra days
                if (neighbor.Plan.IsNurtured || neighbor.State.CropId == context.State.CropId || CropTraits.traitManager.IgnoresGrowthModifiers(neighbor.State.Traits))
                {
                    continue;
                }

                if (context.Random.NextDouble() < chance)
                {
                    neighbor.Plan.IsNurtured = true;
                    neighbor.Plan.ExtraGrowthDays++;
                    boostedCount++;
                }
            }

            return boostedCount;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { boostChance = ToPercentValue(BOOST_CHANCE_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
