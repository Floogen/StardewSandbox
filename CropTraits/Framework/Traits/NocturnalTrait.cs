using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    public class NocturnalTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Nocturnal";

        public const int NIGHT_START_TIME = 1800;
        public const double NIGHT_STEP_UP_CHANCE = 0.50;
        public const double DAY_STEP_DOWN_CHANCE = 0.25;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        public override int ModifyHarvestQuality(int quality, Random random, int tier)
        {
            if (Game1.timeOfDay >= NIGHT_START_TIME)
            {
                return random.NextDouble() < NIGHT_STEP_UP_CHANCE ? QualityHelper.ShiftQuality(quality, 1) : quality;
            }

            return random.NextDouble() < DAY_STEP_DOWN_CHANCE ? QualityHelper.ShiftQuality(quality, -1) : quality;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { nightStepUpChance = ToPercentValue(NIGHT_STEP_UP_CHANCE), dayStepDownChance = ToPercentValue(DAY_STEP_DOWN_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
