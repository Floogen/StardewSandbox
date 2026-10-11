using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // A gentler Withering. The ready days are counted overnight and applied at harvest by the HarvestManager
    public class FragileTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Fragile";

        // Withering and Ephemeral kill the crop the first night it's left ready, so Fragile would never apply
        private static readonly string[] CONFLICTS = { WitheringTrait.ID, EphemeralTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override int MaxTier => 1;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool TracksDaysLeftReady => true;

        public override int GetLeftReadyQualityShift(int daysLeftReady, int tier)
        {
            return -daysLeftReady;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details");
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
