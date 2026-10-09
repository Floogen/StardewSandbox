using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;

namespace CropTraits.Framework.Traits
{
    // Fused from Waterside and Rain Loving, treating the crop like rice when it's near water
    public class PaddyTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Paddy";

        public const double NEAR_WATER_BOOST_CHANCE = 0.25;

        public override string Id => ID;
        public override string FirstTraitId => WatersideTrait.ID;
        public override string SecondTraitId => RainLovingTrait.ID;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors is false || WaterHelper.IsNearWater(context.Location, context.Tile) is false)
            {
                return;
            }

            context.Plan.CountsAsWatered = true;
            if (context.Random.NextDouble() < NEAR_WATER_BOOST_CHANCE)
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Near water, never needs watering and has a {ToPercent(NEAR_WATER_BOOST_CHANCE)} chance to grow an extra day each night";
        }

        public override string GetNotes()
        {
            return $"Only applies outdoors. Near water means within {WaterHelper.WATER_RANGE} tiles of any water tile (fish ponds don't count). The soil still looks dry. Away from water it grows normally.";
        }
    }
}
