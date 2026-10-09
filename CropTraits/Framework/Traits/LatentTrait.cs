namespace CropTraits.Framework.Traits
{
    // Awakened by the TraitManager whenever it comes out of the Seed Splicer, whether crossbred or diluted
    public class LatentTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Latent";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 1;
        public override bool AwakensWhenSpliced => true;

        public override string GetTierDetails(int tier)
        {
            return "Does nothing on its own. When it comes out of the Seed Splicer, it turns into a random positive trait at tier I";
        }

        public override string GetNotes()
        {
            return "Picks from traits the crop can mutate into that it doesn't already have and wouldn't conflict with. If none fit, it stays Latent. The Seed Maker keeps it Latent.";
        }
    }
}
