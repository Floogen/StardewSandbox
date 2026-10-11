using System;

namespace CropTraits.Framework.Traits
{
    // Fused from Erratic and Volatile
    public class ChaoticTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Chaotic";

        public const double IRIDIUM_CHANCE = 0.35;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => ErraticTrait.ID;
        public override string SecondTraitId => VolatileTrait.ID;

        public override int ModifyHarvestQuality(int quality, Random random, int tier)
        {
            if (quality != StardewValley.Object.medQuality && quality != StardewValley.Object.highQuality)
            {
                return quality;
            }

            return random.NextDouble() < IRIDIUM_CHANCE ? StardewValley.Object.bestQuality : StardewValley.Object.lowQuality;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { iridiumChance = ToPercentValue(IRIDIUM_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
