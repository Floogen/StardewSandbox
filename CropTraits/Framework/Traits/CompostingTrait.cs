using CropTraits.Framework.Models;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;

namespace CropTraits.Framework.Traits
{
    public class CompostingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Composting";

        // Basic, Quality then Deluxe Fertilizer, ranked weakest to strongest
        public static readonly string[] FERTILIZER_IDS_BY_TIER = { "(O)368", "(O)369", "(O)919" };
        public static readonly string[] FERTILIZER_NAMES_BY_TIER = { "Basic Fertilizer", "Quality Fertilizer", "Deluxe Fertilizer" };

        public const int NO_FERTILIZER_RANK = 0;
        public const int OTHER_FERTILIZER_RANK = -1;

        public override string Id => ID;

        public override void OnCropRemovedAfterHarvest(HarvestContext context, int tier)
        {
            HoeDirt soil = context.Soil;
            if (soil is null)
            {
                return;
            }

            // Other fertilizers (like Speed-Gro or Mutagenic Fertilizer) are left alone, as are stronger quality fertilizers
            int currentRank = GetFertilizerRank(soil.fertilizer.Value);
            int compostRank = Math.Clamp(tier, 1, FERTILIZER_IDS_BY_TIER.Length);
            if (currentRank == OTHER_FERTILIZER_RANK || currentRank >= compostRank)
            {
                return;
            }

            soil.fertilizer.Value = FERTILIZER_IDS_BY_TIER[compostRank - 1];
        }

        // Fertilizer may be stored qualified or unqualified
        public static int GetFertilizerRank(string fertilizerId)
        {
            if (string.IsNullOrEmpty(fertilizerId))
            {
                return NO_FERTILIZER_RANK;
            }

            int index = Array.IndexOf(FERTILIZER_IDS_BY_TIER, ItemRegistry.QualifyItemId(fertilizerId));
            return index < 0 ? OTHER_FERTILIZER_RANK : index + 1;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Leaves {FERTILIZER_NAMES_BY_TIER[Math.Clamp(tier, 1, FERTILIZER_NAMES_BY_TIER.Length) - 1]} in its soil once it's harvested for the last time";
        }

        public override string GetNotes()
        {
            return "Only upgrades soil with no fertilizer or a weaker quality fertilizer. Other fertilizers like Speed-Gro are left alone. A crop that grows back leaves it after its final harvest.";
        }
    }
}
