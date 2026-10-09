using CropTraits.Framework.Managers;

namespace CropTraits.Framework.Traits
{
    // Only protects against mutation, so dilution can still remove traits (including Stable itself)
    public class StableTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Stable";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override bool BlocksMutationReplacement => true;

        public override string GetNotes()
        {
            return $"Mutations can still add a trait (when the crop has fewer than {TraitManager.MAX_TRAITS}) or raise a tier. Crossbreeding can still remove traits, including Stable.";
        }
    }
}
