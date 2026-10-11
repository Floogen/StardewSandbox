namespace CropTraits.Framework.Traits
{
    // Fused from Ripe and Stubborn, a neutral trade of time for value
    public class CuredTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Cured";

        public const double TIME_INCREASE = 0.50;
        public const double ARTISAN_PRICE_BONUS = 0.25;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => RipeTrait.ID;
        public override string SecondTraitId => StubbornTrait.ID;

        // A price trait so it carries into the artisan goods, though the produce itself sells as normal
        public override bool IsPriceTrait => true;

        public override double ModifyMachineTime(double multiplier, int tier)
        {
            return multiplier * (1 + TIME_INCREASE);
        }

        public override double ModifyArtisanSellPrice(double multiplier, int tier)
        {
            return multiplier * (1 + ARTISAN_PRICE_BONUS);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { timeIncrease = ToPercentValue(TIME_INCREASE), artisanPriceBonus = ToPercentValue(ARTISAN_PRICE_BONUS) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
