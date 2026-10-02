namespace CropTraits.Framework.Utilities.Helpers
{
    public static class QualityHelper
    {
        public const double BONUS_PER_QUALITY_STEP = 0.05;
        public const int IRIDIUM_QUALITY = 4;
        public const int IRIDIUM_QUALITY_STEP = 3;

        // Quality values are 0, 1, 2 and 4, so iridium maps to step 3
        public static int GetQualityStep(int quality)
        {
            return quality >= IRIDIUM_QUALITY ? IRIDIUM_QUALITY_STEP : quality;
        }

        public static double GetBonus(int quality)
        {
            return GetQualityStep(quality) * BONUS_PER_QUALITY_STEP;
        }
    }
}
