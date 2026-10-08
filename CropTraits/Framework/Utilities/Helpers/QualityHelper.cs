using System;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class QualityHelper
    {
        public const double BONUS_PER_QUALITY_STEP = 0.05;
        public const int IRIDIUM_QUALITY = 4;
        public const int IRIDIUM_QUALITY_STEP = 3;

        // Quality values in order (normal, silver, gold then iridium), since iridium skips a value
        public static readonly int[] QUALITY_VALUES = { StardewValley.Object.lowQuality, StardewValley.Object.medQuality, StardewValley.Object.highQuality, StardewValley.Object.bestQuality };

        // Quality values are 0, 1, 2 and 4, so iridium maps to step 3
        public static int GetQualityStep(int quality)
        {
            return quality >= IRIDIUM_QUALITY ? IRIDIUM_QUALITY_STEP : quality;
        }

        public static double GetBonus(int quality)
        {
            return GetQualityStep(quality) * BONUS_PER_QUALITY_STEP;
        }

        // Moves a quality up (positive) or down (negative) by whole steps, staying between normal and iridium
        public static int ShiftQuality(int quality, int steps)
        {
            int stepIndex = Array.IndexOf(QUALITY_VALUES, quality);
            if (stepIndex < 0)
            {
                return quality;
            }

            return QUALITY_VALUES[Math.Clamp(stepIndex + steps, 0, QUALITY_VALUES.Length - 1)];
        }
    }
}
