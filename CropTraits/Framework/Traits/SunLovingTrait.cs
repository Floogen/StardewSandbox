using CropTraits.Framework.Models;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The mirror of Rain Loving. Dry days are far more common than rainy ones, so the bonus is a chance while the rain penalty is certain
    public class SunLovingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "SunLoving";

        public const double DRY_DAY_BOOST_CHANCE = 0.25;

        private static readonly string[] CONFLICTS = { RainLovingTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Uses the weather of the day that's ending, since that's the day the crop grows from overnight
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors is false)
            {
                return;
            }

            if (context.IsRaining)
            {
                context.Plan.SkipsGrowth = true;
                return;
            }

            if (context.Random.NextDouble() < DRY_DAY_BOOST_CHANCE)
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(DRY_DAY_BOOST_CHANCE)} chance to grow an extra day after days without rain, but skips growing after rainy days";
        }

        public override string GetNotes()
        {
            return "Only applies outdoors. Storms count as rain. The extra day only counts on nights the crop grows (when it's watered).";
        }
    }
}
