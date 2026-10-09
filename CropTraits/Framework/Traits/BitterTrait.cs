namespace CropTraits.Framework.Traits
{
    public class BitterTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Bitter";

        public const double PRICE_PENALTY_PER_TIER = 0.15;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override bool IsPriceTrait => true;

        public override double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier * (1 - (PRICE_PENALTY_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return $"Sells for {ToPercent(PRICE_PENALTY_PER_TIER * tier)} less";
        }
    }
}
