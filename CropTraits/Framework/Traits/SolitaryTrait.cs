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
            return $"Has a {ToPercent(ALONE_BOOST_CHANCE)} chance to grow an extra day each night with {MAX_ALONE_NEIGHBORS} or fewer crops around it, but a {ToPercent(CROWDED_SKIP_CHANCE)} chance to skip growing each night with {MIN_CROWDED_NEIGHBORS} or more";
        }

        public override string GetNotes()
        {
            return $"Covers all eight surrounding tiles, counting crops of any type. With {MAX_ALONE_NEIGHBORS + 1} to {MIN_CROWDED_NEIGHBORS - 1} crops around it, it grows normally. The extra day only counts on nights the crop grows (when it's watered).";
        }
    }
}
