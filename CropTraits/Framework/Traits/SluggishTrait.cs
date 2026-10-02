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
        public override bool IsNegative => true;
        public override int Weight => 12;

        public override void ModifyGrowth(IList<int> phaseDays, CropData cropData, int tier)
        {
            GrowthHelper.AddGrowthDays(phaseDays, (int)Math.Ceiling(GrowthHelper.GetGrowthDays(phaseDays) * SLOWDOWN_PER_TIER * tier));
        }
    }
}
