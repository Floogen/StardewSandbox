namespace CropTraits.Framework.Traits
{
    public class RipeTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Ripe";

        public const double TIME_REDUCTION_PER_TIER = 0.10;

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;

        public override double ModifyMachineTime(double multiplier, int tier)
        {
            return multiplier * (1 - (TIME_REDUCTION_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { timeReduction = ToPercentValue(TIME_REDUCTION_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
