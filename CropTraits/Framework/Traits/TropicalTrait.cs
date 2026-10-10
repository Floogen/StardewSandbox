using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    // Fused from Hothouse and Sun Loving, thriving anywhere warm without being held back elsewhere
    public class TropicalTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Tropical";

        public const double BOOST_CHANCE = 0.25;

        public override string Id => ID;
        public override string FirstTraitId => HothouseTrait.ID;
        public override string SecondTraitId => SunLovingTrait.ID;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if ((context.IsOutdoors is false || context.Location.InIslandContext()) && context.Random.NextDouble() < BOOST_CHANCE)
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(BOOST_CHANCE)} chance to grow an extra day each night indoors or anywhere on Ginger Island";
        }

        public override string GetNotes()
        {
            return "Grows normally everywhere else, without Hothouse's outdoor penalty or Sun Loving's rain penalty. The extra day only counts on nights the crop grows (when it's watered).";
        }
    }
}
