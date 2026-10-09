namespace CropTraits.Framework.Traits
{
    public class LuminousTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Luminous";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override bool GlowsAtNight => true;

        public override string GetTierDetails(int tier)
        {
            return "Glows softly at night";
        }

        public override string GetNotes()
        {
            return "Only the planted crop glows, not its produce.";
        }
    }
}
