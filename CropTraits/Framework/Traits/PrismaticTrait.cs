using Microsoft.Xna.Framework;
using StardewValley;

namespace CropTraits.Framework.Traits
{
    // Fused from Luminous and Variegated
    public class PrismaticTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Prismatic";

        public const double PRICE_BONUS = 0.10;

        public override string Id => ID;
        public override string FirstTraitId => LuminousTrait.ID;
        public override string SecondTraitId => VariegatedTrait.ID;
        public override bool GlowsAtNight => true;
        public override bool IsPriceTrait => true;

        public override Color ModifyTint(Color tint, string seedId, int tier)
        {
            return Utility.GetPrismaticColor();
        }

        public override double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier * (1 + PRICE_BONUS);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { priceBonus = ToPercentValue(PRICE_BONUS) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
