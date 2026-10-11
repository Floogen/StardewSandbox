using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    public class SolitaryTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Solitary";

        public const double ALONE_BOOST_CHANCE = 0.25;
        public const double CROWDED_SKIP_CHANCE = 0.25;
        public const int MAX_ALONE_NEIGHBORS = 2; // A single row or the edge of a path still counts as alone
        public const int MIN_CROWDED_NEIGHBORS = 6; // The inside of a packed block (such as under a sprinkler) counts as crowded

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        // Neighbors are any living crop in the eight surrounding tiles, traited or not. Counts between the two bands grow normally
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            int neighborCount = context.Neighbors.Count;
            if (neighborCount <= MAX_ALONE_NEIGHBORS)
            {
                if (context.Random.NextDouble() < ALONE_BOOST_CHANCE)
                {
                    context.Plan.ExtraGrowthDays++;
                }

                return;
            }

            if (neighborCount >= MIN_CROWDED_NEIGHBORS && context.Random.NextDouble() < CROWDED_SKIP_CHANCE)
            {
                context.Plan.SkipsGrowth = true;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { aloneBoostChance = ToPercentValue(ALONE_BOOST_CHANCE), maxAloneNeighbors = MAX_ALONE_NEIGHBORS, crowdedSkipChance = ToPercentValue(CROWDED_SKIP_CHANCE), minCrowdedNeighbors = MIN_CROWDED_NEIGHBORS });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { normalMin = (MAX_ALONE_NEIGHBORS + 1), normalMax = (MIN_CROWDED_NEIGHBORS - 1) });
        }
    }
}
