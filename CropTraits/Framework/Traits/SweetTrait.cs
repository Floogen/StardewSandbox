using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class SweetTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Sweet";

        public const double PRICE_BONUS_PER_TIER = 0.15;

        private static readonly string[] CONFLICTS = { BitterTrait.ID };

        public override string Id => ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool IsPriceTrait => true;

        public override double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier * (1 + (PRICE_BONUS_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return $"Sells for {ToPercent(PRICE_BONUS_PER_TIER * tier)} more";
        }
    }
}
