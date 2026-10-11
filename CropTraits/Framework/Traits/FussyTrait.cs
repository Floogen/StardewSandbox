namespace CropTraits.Framework.Traits
{
    public class FussyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Fussy";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override bool IgnoresFertilizer => true;

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
