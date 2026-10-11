using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    public class HothouseTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Hothouse";

        public const double INDOOR_BOOST_CHANCE = 0.25;
        public const double OUTDOOR_SKIP_CHANCE = 0.25;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors)
            {
                if (context.Random.NextDouble() < OUTDOOR_SKIP_CHANCE)
                {
                    context.Plan.SkipsGrowth = true;
                }

                return;
            }

            if (context.Random.NextDouble() < INDOOR_BOOST_CHANCE)
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { indoorBoostChance = ToPercentValue(INDOOR_BOOST_CHANCE), outdoorSkipChance = ToPercentValue(OUTDOOR_SKIP_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
