using System;

namespace CropTraits.Framework.Traits
{
    public class ErraticTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Erratic";

        public const double STEP_UP_CHANCE = 0.5;

        // Quality steps in order, since iridium skips a value
        public static readonly int[] QUALITY_STEPS = { StardewValley.Object.lowQuality, StardewValley.Object.medQuality, StardewValley.Object.highQuality, StardewValley.Object.bestQuality };

        public override string Id => ID;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Uncommon;

        public override int ModifyHarvestQuality(int quality, Random random, int tier)
        {
            int stepIndex = Array.IndexOf(QUALITY_STEPS, quality);
            if (stepIndex < 0)
            {
                return quality;
            }

            int shiftedIndex = random.NextDouble() < STEP_UP_CHANCE ? stepIndex + 1 : stepIndex - 1;
            return QUALITY_STEPS[Math.Clamp(shiftedIndex, 0, QUALITY_STEPS.Length - 1)];
        }

        public override string GetTierDetails(int tier)
        {
            return "Harvest quality moves one step up or down at random";
        }

        public override string GetNotes()
        {
            return "Steps go normal, silver, gold then iridium. Quality never goes below normal or past the crop's own limits.";
        }
    }
}
