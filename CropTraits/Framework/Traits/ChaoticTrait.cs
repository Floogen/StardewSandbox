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
            return $"Silver and gold harvests become iridium or normal ({ToPercent(IRIDIUM_CHANCE)} chance of iridium)";
        }

        public override string GetNotes()
        {
            return "Normal and iridium harvests stay as they are. Quality never goes past the crop's own limits.";
        }
    }
}
