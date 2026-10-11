using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    public class RainLovingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "RainLoving";

        public const double DRY_DAY_SKIP_CHANCE = 0.25;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        // Uses the weather of the day that's ending, since that's the day the crop grows from overnight
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors is false)
            {
                return;
            }

            if (context.IsRaining)
            {
                context.Plan.ExtraGrowthDays++;
                return;
            }

            if (context.Random.NextDouble() < DRY_DAY_SKIP_CHANCE)
            {
                context.Plan.SkipsGrowth = true;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { dryDaySkipChance = ToPercentValue(DRY_DAY_SKIP_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
