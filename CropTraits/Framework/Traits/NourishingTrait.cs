using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    public class NourishingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Nourishing";

        public const double RESTORATION_BONUS_PER_TIER = 0.25;

        public override string Id => ID;

        public override bool CanApplyTo(CropData cropData)
        {
            return CropDataHelper.HasEdibleHarvest(cropData);
        }

        public override double ModifyFoodRestoration(double multiplier, int tier)
        {
            return multiplier * (1 + (RESTORATION_BONUS_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return $"Restores {ToPercent(RESTORATION_BONUS_PER_TIER * tier)} more energy and health when eaten";
        }

        public override string GetNotes()
        {
            return "Only appears on crops with edible produce.";
        }
    }
}
