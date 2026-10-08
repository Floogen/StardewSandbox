using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    public class GreedyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Greedy";

        public const double SLOW_CHANCE_PER_TIER = 0.10;

        public override string Id => ID;
        public override bool IsNegative => true;
        public override TraitRarity Rarity => TraitRarity.Uncommon;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            SlowNeighbors(context, SLOW_CHANCE_PER_TIER * tier);
        }

        // Rolls once for each adjacent crop of another type (in a fixed order), returning how many were slowed
        public static int SlowNeighbors(NightlyContext context, double chance)
        {
            int slowedCount = 0;
            foreach (NightlyNeighbor neighbor in context.Neighbors)
            {
                if (neighbor.State.CropId == context.State.CropId || CropTraits.traitManager.IgnoresGrowthModifiers(neighbor.State.Traits))
                {
                    continue;
                }

                if (context.Random.NextDouble() < chance)
                {
                    neighbor.Plan.SkipsGrowth = true;
                    slowedCount++;
                }
            }

            return slowedCount;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Each adjacent crop of another type has a {ToPercent(SLOW_CHANCE_PER_TIER * tier)} chance to skip growing each night";
        }

        public override string GetNotes()
        {
            return "Covers all eight surrounding tiles. Crops with Balanced aren't affected.";
        }
    }
}
