using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    // The opposite of Resprouting, turning a regrowing crop into a single harvest crop
    public class SpentTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Spent";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 1;
        public override bool IsRemovedAfterHarvest => true;

        // Only crops that regrow have anything to lose
        public override bool CanApplyTo(CropData cropData)
        {
            return cropData is not null && cropData.RegrowDays > 0;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details");
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
