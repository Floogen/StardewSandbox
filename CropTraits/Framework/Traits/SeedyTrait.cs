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
            return GetText("details", new { extraSeedChance = ToPercentValue(EXTRA_SEED_CHANCE_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
