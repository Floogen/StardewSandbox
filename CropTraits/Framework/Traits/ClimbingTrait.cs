using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    // Handled by PassabilityHelper, making a crop block the way like a trellis crop
    public class ClimbingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Climbing";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        // Trellis crops already block the way
        public override bool CanApplyTo(CropData cropData)
        {
            return cropData is not null && cropData.IsRaised is false;
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
