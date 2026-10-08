namespace CropTraits.Framework.Traits
{
    public class ResilientTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Resilient";

        public override string Id => ID;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override bool GuaranteesDilutionSplit => true;

        public override string GetTierDetails(int tier)
        {
            return "Diluting it in the Seed Splicer always splits off one trait instead of sometimes wiping them all";
        }

        public override string GetNotes()
        {
            return "The trait removed is still picked evenly, so Resilient itself can be the one removed.";
        }
    }
}
