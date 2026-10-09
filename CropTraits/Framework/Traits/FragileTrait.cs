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
        public override bool LosesQualityWhenLeftReady => true;

        public override string GetTierDetails(int tier)
        {
            return "Harvest quality drops one step for each full day it's left ready";
        }

        public override string GetNotes()
        {
            return "Harvesting on the day it's ready loses nothing. Steps go iridium, gold, silver then normal, and quality never goes below normal.";
        }
    }
}
