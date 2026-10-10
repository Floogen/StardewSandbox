using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class LargeTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Large";

        public const float SIZE_INCREASE = 0.10f;
        public const double PRICE_BONUS = 0.25;

        private static readonly string[] CONFLICTS = { SmallTrait.ID };

        public override string Id => ID;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool IsPriceTrait => true;

        public override float ModifyProduceScale(float scale, int tier)
        {
            return scale * (1 + SIZE_INCREASE);
        }

        public override double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier * (1 + PRICE_BONUS);
        }

        public override string GetTierDetails(int tier)
        {
            return $"{ToPercent(SIZE_INCREASE)} bigger and sells for {ToPercent(PRICE_BONUS)} more";
        }
    }
}
