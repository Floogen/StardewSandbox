using CropTraits.Framework.Models;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class ThirstyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Thirsty";

        private static readonly string[] CONFLICTS = { DroughtTolerantTrait.ID, DeepRootedTrait.ID };

        public override string Id => ID;
        public override bool IsNegative => true;
        public override int MaxTier => 1;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override bool CanApplyTo(CropData cropData)
        {
            return cropData?.NeedsWatering ?? false;
        }

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            context.Plan.LosesGrowthWhenDry = true;
        }

        public override string GetTierDetails(int tier)
        {
            return "Loses a day of growth when left unwatered";
        }

        public override string GetNotes()
        {
            return "Growth never goes back past the day it was planted. Crops ready to harvest aren't affected.";
        }
    }
}
