using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Seedy
    public class BarrenTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Barren";

        public const double FEWER_SEED_CHANCE_PER_TIER = 0.25;

        private static readonly string[] CONFLICTS = { SeedyTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override int GetExtraSeeds(Random random, int tier)
        {
            return random.NextDouble() < FEWER_SEED_CHANCE_PER_TIER * tier ? -1 : 0;
        }

        public override string GetTierDetails(int tier)
        {
            return $"{ToPercent(FEWER_SEED_CHANCE_PER_TIER * tier)} chance for the Seed Maker to make one fewer seed";
        }

        public override string GetNotes()
        {
            return "The Seed Maker always makes at least one seed. Only applies when it makes the crop's own seeds, not Mixed or Ancient Seeds.";
        }
    }
}
