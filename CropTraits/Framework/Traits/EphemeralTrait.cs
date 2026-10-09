namespace CropTraits.Framework.Traits
{
    // Fused from Withering and Regal. Since it withers the night after it's ready, every harvest gets the boost
    public class EphemeralTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Ephemeral";

        public const int QUALITY_LEVELS = 3;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => WitheringTrait.ID;
        public override string SecondTraitId => RegalTrait.ID;
        public override bool WithersWhenLeftReady => true;

        public override int ModifyQualityBoostLevel(int level, int tier)
        {
            return level + QUALITY_LEVELS;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Quality odds raised by {QUALITY_LEVELS} fertilizer levels, but withers if not harvested on the day it's ready";
        }

        public override string GetNotes()
        {
            return "Each quality level matches one step of fertilizer (Basic, Quality then Deluxe) and stacks with any fertilizer used.";
        }
    }
}
