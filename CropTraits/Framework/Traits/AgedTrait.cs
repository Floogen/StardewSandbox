namespace CropTraits.Framework.Traits
{
    // Fused from Ripe and Vintage, covering the casks Ripe can't speed up
    public class AgedTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Aged";

        public const double AGING_BONUS = 0.50;

        public override string Id => ID;
        public override string FirstTraitId => RipeTrait.ID;
        public override string SecondTraitId => VintageTrait.ID;

        // A price trait so it carries into the wine or juice made from it, which is what goes in the cask
        public override bool IsPriceTrait => true;

        public override double ModifyCaskAgingRate(double multiplier, int tier)
        {
            return multiplier * (1 + AGING_BONUS);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Casks age artisan goods made from it {ToPercent(AGING_BONUS)} faster";
        }

        public override string GetNotes()
        {
            return "Carries into the artisan goods made from it, so only those goods age faster. It doesn't change their price.";
        }
    }
}
