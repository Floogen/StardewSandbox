namespace CropTraits.Framework.Traits
{
    // Fused from Colossal and Prolific, keeping a Colossal II bonus so the combo still forms giant crops
    public class TitanicTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Titanic";

        public const double GIANT_CROP_CHANCE = 0.04;
        public const double YIELD_BONUS = 0.50;

        public override string Id => ID;
        public override string FirstTraitId => ColossalTrait.ID;
        public override string SecondTraitId => ProlificTrait.ID;

        public override double ModifyGiantCropChance(double chance, int tier)
        {
            return chance + GIANT_CROP_CHANCE;
        }

        public override double ModifyGiantCropYield(double multiplier, int tier)
        {
            return multiplier * (1 + YIELD_BONUS);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { giantCropChance = ToPercentValue(GIANT_CROP_CHANCE), yieldBonus = ToPercentValue(YIELD_BONUS) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
