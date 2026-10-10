using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Fragile. The ready days are counted overnight and applied at harvest by the HarvestManager
    public class MellowTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Mellow";

        public const int DAYS_PER_STEP = 2;

        // Fragile pulls the other way, while Withering and Ephemeral kill the crop the first night it's left ready
        private static readonly string[] CONFLICTS = { FragileTrait.ID, WitheringTrait.ID, EphemeralTrait.ID };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool TracksDaysLeftReady => true;

        public override int GetLeftReadyQualityShift(int daysLeftReady, int tier)
        {
            return Math.Min(daysLeftReady / DAYS_PER_STEP, tier);
        }

        public override string GetTierDetails(int tier)
        {
            return tier == 1 ? $"Harvest quality rises one step once it's left ready for {DAYS_PER_STEP} full days" : $"Harvest quality rises one step for every {DAYS_PER_STEP} full days it's left ready (up to {tier} steps)";
        }

        public override string GetNotes()
        {
            return "Steps go normal, silver, gold then iridium. Quality never goes past the crop's own limits.";
        }
    }
}
