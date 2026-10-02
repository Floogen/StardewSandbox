namespace CropTraits.Framework.Traits
{
    public class BitterTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Bitter";

        public const double PRICE_PENALTY_PER_TIER = 0.15;

        public override string Id => ID;
        public override bool IsNegative => true;
        public override int Weight => 12;
        public override bool IsPriceTrait => true;

        public override double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier * (1 - (PRICE_PENALTY_PER_TIER * tier));
        }
    }
}
