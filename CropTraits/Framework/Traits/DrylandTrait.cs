using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The mirror of Waterside
    public class DrylandTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Dryland";

        public const double AWAY_FROM_WATER_BOOST_CHANCE = 0.25;
        public const double NEAR_WATER_SKIP_CHANCE = 0.25;

        private static readonly string[] CONFLICTS = { WatersideTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors is false)
            {
                return;
            }

            if (WaterHelper.IsNearWater(context.Location, context.Tile))
            {
                if (context.Random.NextDouble() < NEAR_WATER_SKIP_CHANCE)
                {
                    context.Plan.SkipsGrowth = true;
                }

                return;
            }

            if (context.Random.NextDouble() < AWAY_FROM_WATER_BOOST_CHANCE)
            {
                context.Plan.ExtraGrowthDays++;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { awayFromWaterBoostChance = ToPercentValue(AWAY_FROM_WATER_BOOST_CHANCE), nearWaterSkipChance = ToPercentValue(NEAR_WATER_SKIP_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { waterRange = WaterHelper.WATER_RANGE });
        }
    }
}
