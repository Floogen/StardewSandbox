using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class CrowMagnetTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "CrowMagnet";

        private static readonly string[] CONFLICTS = { ThornyTrait.ID, GuardianTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool IgnoresScarecrows => true;

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
