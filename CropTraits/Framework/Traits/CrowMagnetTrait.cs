using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class CrowMagnetTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "CrowMagnet";

        private static readonly string[] CONFLICTS = { ThornyTrait.ID, GuardianTrait.ID };

        public override string Id => ID;
        public override bool IsNegative => true;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool IgnoresScarecrows => true;

        public override string GetTierDetails(int tier)
        {
            return "Scarecrows don't protect it";
        }

        public override string GetNotes()
        {
            return "Crops with Guardian can't protect it either.";
        }
    }
}
