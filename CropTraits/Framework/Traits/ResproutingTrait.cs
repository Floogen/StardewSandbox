using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    public class ResproutingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Resprouting";

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Rare;

        // Crops that already regrow have nothing to gain
        public override bool CanApplyTo(CropData cropData)
        {
            return cropData is not null && cropData.RegrowDays <= 0;
        }

        public override int GetExtraRegrowths(int tier)
        {
            return tier;
        }

        public override string GetTierDetails(int tier)
        {
            return tier == 1 ? "Grows back once after harvest" : $"Grows back {tier} times after harvest";
        }

        public override string GetNotes()
        {
            return "Only appears on crops that don't already regrow. Each time, it goes back to the growth stage halfway through, so growing back takes about half the original time.";
        }
    }
}
