namespace CropTraits.Framework.Traits
{
    public class ResilientTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Resilient";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override bool GuaranteesDilutionSplit => true;

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
