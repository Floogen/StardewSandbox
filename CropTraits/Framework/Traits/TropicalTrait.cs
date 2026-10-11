using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    // Fused from Hothouse and Sun Loving, thriving anywhere warm and on dry days elsewhere without either parent's penalty
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
            // Indoors and Ginger Island are always warm, while elsewhere outdoors only dry days count
            bool isWarm = context.IsOutdoors is false || context.Location.InIslandContext() || context.IsRaining is false;
            if (isWarm && context.Random.NextDouble() < BOOST_CHANCE)
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(BOOST_CHANCE)} chance to grow an extra day each night indoors or on Ginger Island, and after dry days everywhere else outdoors";
        }

        public override string GetNotes()
        {
            return "Rain outside Ginger Island only cancels the boost for that night, without Hothouse's outdoor penalty or Sun Loving's rain penalty. Storms count as rain. The extra day only counts on nights the crop grows (when it's watered).";
        }
    }
}
