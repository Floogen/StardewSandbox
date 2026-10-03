namespace CropTraits.Framework.Traits
{
    public class SmallTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Small";

        public const float SIZE_REDUCTION = 0.25f;
        public const double PRICE_PENALTY = 0.15;

        public override string Id => ID;
        public override bool IsNegative => true;
        public override int MaxTier => 1;
        public override int Weight => 12;
        public override bool IsPriceTrait => true;

        public override float ModifyProduceScale(float scale, int tier)
        {
            return scale * (1 - SIZE_REDUCTION);
        }

        public override double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier * (1 - PRICE_PENALTY);
        }
    }
}
