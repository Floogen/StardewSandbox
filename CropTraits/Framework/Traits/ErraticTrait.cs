using CropTraits.Framework.Utilities.Helpers;
using System;

namespace CropTraits.Framework.Traits
{
    public class ErraticTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Erratic";

        public const double STEP_UP_CHANCE = 0.5;

        public override string Id => ID;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Uncommon;

        public override int ModifyHarvestQuality(int quality, Random random, int tier)
        {
            return QualityHelper.ShiftQuality(quality, random.NextDouble() < STEP_UP_CHANCE ? 1 : -1);
        }

        public override string GetTierDetails(int tier)
        {
            return "Harvest quality moves one step up or down at random";
        }

        public override string GetNotes()
        {
            return "Steps go normal, silver, gold then iridium. Quality never goes below normal or past the crop's own limits.";
        }
    }
}
