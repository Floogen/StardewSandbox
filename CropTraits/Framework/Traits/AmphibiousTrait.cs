using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    // Fused from Waterside and Dryland, growing well near water or away from it
    public class AmphibiousTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Amphibious";

        public const double BOOST_CHANCE = 0.25;

        public override string Id => ID;
        public override string FirstTraitId => WatersideTrait.ID;
        public override string SecondTraitId => DrylandTrait.ID;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors && context.Random.NextDouble() < BOOST_CHANCE)
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(BOOST_CHANCE)} chance to grow an extra day each night outdoors, near water or not";
        }

        public override string GetNotes()
        {
            return "Only applies outdoors. The extra day only counts on nights the crop grows (when it's watered).";
        }
    }
}
