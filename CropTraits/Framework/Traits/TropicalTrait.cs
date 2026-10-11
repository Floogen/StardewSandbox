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
            return GetText("details", new { boostChance = ToPercentValue(BOOST_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
