using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using System;

namespace CropTraits.Framework.Traits
{
    // Fused from Seasoned and Hardy, outlasting its season and growing better for it
    public class EnduringTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Enduring";

        public const int OUT_OF_SEASON_NIGHTS = 6;

        public override string Id => ID;
        public override string FirstTraitId => SeasonedTrait.ID;
        public override string SecondTraitId => HardyTrait.ID;

        public override int GetOutOfSeasonNights(int tier)
        {
            return OUT_OF_SEASON_NIGHTS;
        }

        public override int ModifyHarvestQuality(int quality, HarvestContext context, Random random, int tier)
        {
            return context.IsOutOfSeason ? QualityHelper.ShiftQuality(quality, 1) : quality;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Keeps growing for {OUT_OF_SEASON_NIGHTS} days after its season ends. Harvests in that time rise one quality step";
        }

        public override string GetNotes()
        {
            return "Only matters outdoors, where crops die when their season ends. Hardy's extra days don't stack with these (the longer one counts). Quality never goes past the crop's own limits.";
        }
    }
}
