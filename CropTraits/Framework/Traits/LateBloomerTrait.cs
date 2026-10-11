using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Sluggish and Regal, trading slower growth for the quality odds of Regal III
    public class LateBloomerTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "LateBloomer";

        public const double SLOWDOWN = 0.20;
        public const int QUALITY_LEVELS = 3;

        // Matches Sluggish, since Late Bloomer replaces it
        private static readonly string[] CONFLICTS = { SwiftTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => SluggishTrait.ID;
        public override string SecondTraitId => RegalTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override void ModifyGrowth(IList<int> phaseDays, CropData cropData, int tier)
        {
            GrowthHelper.AddGrowthDays(phaseDays, (int)Math.Ceiling(GrowthHelper.GetGrowthDays(phaseDays) * SLOWDOWN));
        }

        public override int ModifyQualityBoostLevel(int level, int tier)
        {
            return level + QUALITY_LEVELS;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { slowdown = ToPercentValue(SLOWDOWN), qualityLevels = QUALITY_LEVELS });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
