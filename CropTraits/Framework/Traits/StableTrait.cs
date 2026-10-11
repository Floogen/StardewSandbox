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

        public override string GetTierDetails(int tier)
        {
            return GetText("details");
        }

        public override string GetNotes()
        {
            return GetText("notes", new { maxTraits = TraitManager.MAX_TRAITS });
        }
    }
}
