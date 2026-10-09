using System;

namespace CropTraits.Framework.Traits
{
    public class SeedyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Seedy";

        public const double EXTRA_SEED_CHANCE_PER_TIER = 0.25;

        public override string Id => ID;

        public override int GetExtraSeeds(Random random, int tier)
        {
            return random.NextDouble() < EXTRA_SEED_CHANCE_PER_TIER * tier ? 1 : 0;
        }

        public override string GetTierDetails(int tier)
        {
            return $"{ToPercent(EXTRA_SEED_CHANCE_PER_TIER * tier)} chance for the Seed Maker to make one extra seed";
        }

        public override string GetNotes()
        {
            return "Only applies when the Seed Maker makes the crop's own seeds, not Mixed or Ancient Seeds.";
        }
    }
}
