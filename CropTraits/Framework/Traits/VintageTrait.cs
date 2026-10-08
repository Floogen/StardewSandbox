using CropTraits.Framework.Utilities.Helpers;
using StardewValley;

namespace CropTraits.Framework.Traits
{
    // Fused from Aromatic and Sweet
    public class VintageTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Vintage";

        public const double ARTISAN_PRICE_BONUS = 0.30;
        public const int QUALITY_STEPS = 1;

        public override string Id => ID;
        public override string FirstTraitId => AromaticTrait.ID;
        public override string SecondTraitId => SweetTrait.ID;
        public override bool IsPriceTrait => true;

        public override double ModifyArtisanSellPrice(double multiplier, int tier)
        {
            return multiplier * (1 + ARTISAN_PRICE_BONUS);
        }

        public override void ModifyArtisanOutput(Item output, int tier)
        {
            if (output is not null)
            {
                output.Quality = QualityHelper.ShiftQuality(output.Quality, QUALITY_STEPS);
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Artisan goods made from it come out one quality level higher and sell for {ToPercent(ARTISAN_PRICE_BONUS)} more";
        }

        public override string GetNotes()
        {
            return "The produce itself sells at its normal price. Wine that starts at a higher quality needs less time in a cask.";
        }
    }
}
