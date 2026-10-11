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
            return tier == 1 ? GetText("details.tier_1") : GetText("details", new { count = tier });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
