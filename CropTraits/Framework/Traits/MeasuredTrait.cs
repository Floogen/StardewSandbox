namespace CropTraits.Framework.Traits
{
    // Fused from Seedy and Barren, a neutral opposite fusion that takes the luck out of the Seed Maker
    public class MeasuredTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Measured";

        public const int SEED_COUNT = 2;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => SeedyTrait.ID;
        public override string SecondTraitId => BarrenTrait.ID;

        public override int? GetFixedSeedCount(int tier)
        {
            return SEED_COUNT;
        }

        public override string GetTierDetails(int tier)
        {
            return $"The Seed Maker always makes exactly {SEED_COUNT} seeds from it";
        }

        public override string GetNotes()
        {
            return "Replaces the game's roll and any extra or missing seeds from traits like Seedy and Barren. Only applies when the Seed Maker makes the crop's own seeds.";
        }
    }
}
