namespace CropTraits.Framework.Traits
{
    // Only given when a crossbreed rolls no traits, never by mutation
    public class SeedlessTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Seedless";

        public const double PRICE_BONUS = 0.10;

        public override string Id => ID;
        public override int MaxTier => 1;
        public override bool IsExclusive => true;
        public override bool CanBeRolled => false;
        public override bool BlocksPropagation => true;
        public override bool IsPriceTrait => true;

        public override double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier * (1 + PRICE_BONUS);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { priceBonus = ToPercentValue(PRICE_BONUS) });
        }
    }
}
