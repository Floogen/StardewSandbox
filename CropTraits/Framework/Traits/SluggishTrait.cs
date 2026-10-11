using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class SluggishTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Sluggish";

        public const double SLOWDOWN_PER_TIER = 0.10;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;

        public override void ModifyGrowth(IList<int> phaseDays, CropData cropData, int tier)
        {
            GrowthHelper.AddGrowthDays(phaseDays, (int)Math.Ceiling(GrowthHelper.GetGrowthDays(phaseDays) * SLOWDOWN_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { slowdown = ToPercentValue(SLOWDOWN_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
