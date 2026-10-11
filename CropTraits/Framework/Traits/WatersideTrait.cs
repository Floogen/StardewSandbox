using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    public class WatersideTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Waterside";

        public const double NEAR_WATER_BOOST_CHANCE = 0.25;
        public const double AWAY_FROM_WATER_SKIP_CHANCE = 0.25;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        // Paddy crops like rice already grow faster near water
        public override bool CanApplyTo(CropData cropData)
        {
            return cropData is not null && cropData.IsPaddyCrop is false;
        }

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors is false)
            {
                return;
            }

            if (WaterHelper.IsNearWater(context.Location, context.Tile))
            {
                if (context.Random.NextDouble() < NEAR_WATER_BOOST_CHANCE)
                {
                    context.Plan.ExtraGrowthDays++;
                }

                return;
            }

            if (context.Random.NextDouble() < AWAY_FROM_WATER_SKIP_CHANCE)
            {
                context.Plan.SkipsGrowth = true;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { nearWaterBoostChance = ToPercentValue(NEAR_WATER_BOOST_CHANCE), awayFromWaterSkipChance = ToPercentValue(AWAY_FROM_WATER_SKIP_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { waterRange = WaterHelper.WATER_RANGE });
        }
    }
}
