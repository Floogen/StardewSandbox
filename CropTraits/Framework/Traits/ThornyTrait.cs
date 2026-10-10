namespace CropTraits.Framework.Traits
{
    public class ThornyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Thorny";

        public override string Id => ID;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override bool IsIgnoredByCrows => true;

        public override string GetTierDetails(int tier)
        {
            return "Crows never eat it";
        }

        public override string GetNotes()
        {
            return "Crows pass over it, so it's never eaten.";
        }
    }
}
