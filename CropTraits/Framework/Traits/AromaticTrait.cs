using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    public class AromaticTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Aromatic";

        public const double ARTISAN_PRICE_BONUS_PER_TIER = 0.20;

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override bool IsPriceTrait => true;

        // Fruit and vegetables are what the Keg and Preserves Jar accept
        public override bool CanApplyTo(CropData cropData)
        {
            return CropDataHelper.HasHarvestCategory(cropData, StardewValley.Object.FruitsCategory, StardewValley.Object.VegetableCategory);
        }

        public override double ModifyArtisanSellPrice(double multiplier, int tier)
        {
            return multiplier * (1 + (ARTISAN_PRICE_BONUS_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return $"Artisan goods made from it sell for {ToPercent(ARTISAN_PRICE_BONUS_PER_TIER * tier)} more";
        }

        public override string GetNotes()
        {
            return "The produce itself sells at its normal price. Only appears on fruit and vegetables.";
        }
    }
}
