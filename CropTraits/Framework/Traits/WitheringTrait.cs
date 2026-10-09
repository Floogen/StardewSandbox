namespace CropTraits.Framework.Traits
{
    public class WitheringTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Withering";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override bool WithersWhenLeftReady => true;

        public override string GetTierDetails(int tier)
        {
            return "Withers if not harvested on the day it's ready";
        }

        public override string GetNotes()
        {
            return "Regrowing crops wither the same way each time they're ready again.";
        }
    }
}
