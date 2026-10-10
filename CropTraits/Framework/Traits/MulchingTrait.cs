using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Invasive. Clearing happens each morning (see WeedHelper), so weeds that spawn overnight never show up around it
    public class MulchingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Mulching";

        private static readonly string[] CONFLICTS = { InvasiveTrait.ID };

        public override string Id => ID;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool ClearsNearbyWeeds => true;

        public override string GetTierDetails(int tier)
        {
            return "Clears weeds from the eight surrounding tiles each morning";
        }

        public override string GetNotes()
        {
            return "Cleared weeds don't drop anything. Also clears weeds spread by nearby Invasive crops.";
        }
    }
}
