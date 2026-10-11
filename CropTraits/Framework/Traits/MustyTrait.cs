using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Aromatic
    public class MustyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Musty";

        public const double ARTISAN_PRICE_PENALTY_PER_TIER = 0.20;

        // Vintage is included since it keeps Aromatic's artisan price boost
        private static readonly string[] CONFLICTS = { AromaticTrait.ID, VintageTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool IsPriceTrait => true;

        // Fruit and vegetables are what the Keg and Preserves Jar accept
        public override bool CanApplyTo(CropData cropData)
        {
            return CropDataHelper.HasHarvestCategory(cropData, StardewValley.Object.FruitsCategory, StardewValley.Object.VegetableCategory);
        }

        public override double ModifyArtisanSellPrice(double multiplier, int tier)
        {
            return multiplier * (1 - (ARTISAN_PRICE_PENALTY_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { artisanPricePenalty = ToPercentValue(ARTISAN_PRICE_PENALTY_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
