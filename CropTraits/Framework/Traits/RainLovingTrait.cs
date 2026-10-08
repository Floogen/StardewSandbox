using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    public class RainLovingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "RainLoving";

        public const double DRY_DAY_SKIP_CHANCE = 0.25;

        public override string Id => ID;
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
            return $"Grows an extra day after rainy days, but has a {ToPercent(DRY_DAY_SKIP_CHANCE)} chance to skip growing after days without rain";
        }

        public override string GetNotes()
        {
            return "Only applies outdoors.";
        }
    }
}
