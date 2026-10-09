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
            return $"Harvests from 6pm onward have a {ToPercent(NIGHT_STEP_UP_CHANCE)} chance to rise one quality step, while earlier harvests have a {ToPercent(DAY_STEP_DOWN_CHANCE)} chance to drop one";
        }

        public override string GetNotes()
        {
            return "Junimos harvest during the day, so they get the daytime odds. Quality never goes below normal or past the crop's own limits.";
        }
    }
}
