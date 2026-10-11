using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Ripe
    public class StubbornTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Stubborn";

        public const double TIME_INCREASE_PER_TIER = 0.10;

        private static readonly string[] CONFLICTS = { RipeTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override double ModifyMachineTime(double multiplier, int tier)
        {
            return multiplier * (1 + (TIME_INCREASE_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { timeIncrease = ToPercentValue(TIME_INCREASE_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
