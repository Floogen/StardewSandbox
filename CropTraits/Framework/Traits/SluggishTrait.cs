using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using System;

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

        public override void ModifyGrowth(Crop crop, int tier)
        {
            GrowthHelper.AddGrowthDays(crop, (int)Math.Ceiling(GrowthHelper.GetGrowthDays(crop) * SLOWDOWN_PER_TIER * tier));
        }
    }
}
