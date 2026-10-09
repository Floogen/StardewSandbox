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
            return $"Its own traits are {ToPercent(INHERIT_REDUCTION_PER_TIER * tier)} less likely to carry over when crossbred";
        }

        public override string GetNotes()
        {
            return "Traits both parents share always carry over as normal. Only this parent's own traits are affected, Recessive included.";
        }
    }
}
