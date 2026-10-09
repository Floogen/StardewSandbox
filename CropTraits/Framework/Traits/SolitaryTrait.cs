using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    public class SolitaryTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Solitary";

        public const double ALONE_BOOST_CHANCE = 0.25;
        public const double CROWDED_SKIP_CHANCE = 0.25;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        // Neighbors are any living crop in the eight surrounding tiles, traited or not
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.Neighbors.Count == 0)
            {
                if (context.Random.NextDouble() < ALONE_BOOST_CHANCE)
                {
                    context.Plan.ExtraGrowthDays++;
                }

                return;
            }

            if (context.Random.NextDouble() < CROWDED_SKIP_CHANCE)
            {
                context.Plan.SkipsGrowth = true;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(ALONE_BOOST_CHANCE)} chance to grow an extra day each night with no crops around it, but a {ToPercent(CROWDED_SKIP_CHANCE)} chance to skip growing each night otherwise";
        }

        public override string GetNotes()
        {
            return "Covers all eight surrounding tiles, counting crops of any type. The extra day only counts on nights the crop grows (when it's watered).";
        }
    }
}
