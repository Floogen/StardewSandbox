using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewValley;

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

        // Only outdoors near water, which is also when Mutagenic Fertilizer can roll without the soil being watered
        public override bool CountsAsWatered(GameLocation location, Vector2 tile, int tier)
        {
            return location.IsOutdoors && WaterHelper.IsNearWater(location, tile);
        }

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (CountsAsWatered(context.Location, context.Tile, tier) is false)
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
            return GetText("details", new { nearWaterBoostChance = ToPercentValue(NEAR_WATER_BOOST_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { waterRange = WaterHelper.WATER_RANGE });
        }
    }
}
