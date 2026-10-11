using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class BlandTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Bland";

        public const double RESTORATION_PENALTY_PER_TIER = 0.25;

        private static readonly string[] CONFLICTS = { NourishingTrait.ID, HeartyTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override bool CanApplyTo(CropData cropData)
        {
            return CropDataHelper.HasEdibleHarvest(cropData);
        }

        public override double ModifyFoodRestoration(double multiplier, int tier)
        {
            return multiplier * (1 - (RESTORATION_PENALTY_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { restorationPenalty = ToPercentValue(RESTORATION_PENALTY_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
