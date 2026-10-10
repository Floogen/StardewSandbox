using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    public class SeasonedTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Seasoned";

        public const int LAST_WEEK_START_DAY = 22;
        public const double STEP_UP_CHANCE_PER_TIER = 0.25;

        public override string Id => ID;

        // Goes by the calendar alone, so it works the same in the Greenhouse and on Ginger Island
        public override int ModifyHarvestQuality(int quality, Random random, int tier)
        {
            if (Game1.dayOfMonth < LAST_WEEK_START_DAY || random.NextDouble() >= STEP_UP_CHANCE_PER_TIER * tier)
            {
                return quality;
            }

            return QualityHelper.ShiftQuality(quality, 1);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Harvests in the last week of the season have a {ToPercent(STEP_UP_CHANCE_PER_TIER * tier)} chance to rise one quality step";
        }

        public override string GetNotes()
        {
            return $"The last week runs from day {LAST_WEEK_START_DAY} to the end of the month, wherever the crop grows. Quality never goes past the crop's own limits.";
        }
    }
}
