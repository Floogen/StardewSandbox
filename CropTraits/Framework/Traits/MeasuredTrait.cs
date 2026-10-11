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
            return GetText("details", new { seedCount = SEED_COUNT });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
