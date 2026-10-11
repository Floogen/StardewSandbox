using CropTraits.Framework.Utilities.Helpers;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Regal. Knocks the harvest down a step after the game picks its quality, so fertilizer still helps
    public class CoarseTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Coarse";

        public const double STEP_DOWN_CHANCE_PER_TIER = 0.15;

        // Late Bloomer and Ephemeral are included since they keep Regal's quality boost
        private static readonly string[] CONFLICTS = { RegalTrait.ID, LateBloomerTrait.ID, EphemeralTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override int ModifyHarvestQuality(int quality, Random random, int tier)
        {
            if (random.NextDouble() >= STEP_DOWN_CHANCE_PER_TIER * tier)
            {
                return quality;
            }

            return QualityHelper.ShiftQuality(quality, -1);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { stepDownChance = ToPercentValue(STEP_DOWN_CHANCE_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
