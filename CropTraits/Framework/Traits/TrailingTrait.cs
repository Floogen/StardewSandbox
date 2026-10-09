using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    // Handled by PassabilityHelper, letting players walk through trellis crops
    public class TrailingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Trailing";

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        // Only trellis crops (like hops, grapes and green beans) block the way to begin with
        public override bool CanApplyTo(CropData cropData)
        {
            return cropData is not null && cropData.IsRaised;
        }

        public override string GetTierDetails(int tier)
        {
            return "Can be walked through, even though it grows on a trellis";
        }

        public override string GetNotes()
        {
            return "Only appears on trellis crops like hops, grapes and green beans. Takes effect once the trait shows on the crop.";
        }
    }
}
