using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class SwiftTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Swift";

        public const double SPEED_PER_TIER = 0.10;

        private static readonly string[] CONFLICTS = { SluggishTrait.ID };

        public override string Id => ID;
        public override int Weight => 10;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Removes a share of the growth days, with each phase keeping at least one day
        public override void ModifyGrowth(IList<int> phaseDays, CropData cropData, int tier)
        {
            GrowthHelper.RemoveGrowthDays(phaseDays, (int)Math.Ceiling(GrowthHelper.GetGrowthDays(phaseDays) * SPEED_PER_TIER * tier));
        }
    }
}
