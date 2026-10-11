using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Heirloom, read by CrossbreedHelper
    public class RecessiveTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Recessive";

        public const double INHERIT_REDUCTION_PER_TIER = 0.25;

        private static readonly string[] CONFLICTS = { HeirloomTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override double ModifyInheritChance(double multiplier, int tier)
        {
            return multiplier * (1 - (INHERIT_REDUCTION_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { inheritReduction = ToPercentValue(INHERIT_REDUCTION_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
