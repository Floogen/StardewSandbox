using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    public class FickleTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Fickle";

        // Matches where the fortune teller on TV moves past a neutral reading
        public const double GOOD_LUCK_THRESHOLD = 0.02;
        public const double BAD_LUCK_THRESHOLD = -0.02;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        // Uses the harvesting player's luck, so Junimos follow the luck of whoever runs the farm
        public override int ModifyHarvestQuality(int quality, Random random, int tier)
        {
            double dailyLuck = Game1.player.DailyLuck;
            if (dailyLuck > GOOD_LUCK_THRESHOLD)
            {
                return QualityHelper.ShiftQuality(quality, 1);
            }

            if (dailyLuck < BAD_LUCK_THRESHOLD)
            {
                return QualityHelper.ShiftQuality(quality, -1);
            }

            return quality;
        }

        public override string GetTierDetails(int tier)
        {
            return "Harvests rise one quality step on lucky days and drop one on unlucky days";
        }

        public override string GetNotes()
        {
            return "Uses the harvesting player's luck for the day. A neutral day leaves the quality alone. Quality never goes below normal or past the crop's own limits.";
        }
    }
}
