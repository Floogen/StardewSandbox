using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;

namespace CropTraits.Framework.Traits
{
    public class CompostingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Composting";

        public override string Id => ID;

        // Other fertilizers (like Speed-Gro or Mutagenic Fertilizer) are left alone, as are stronger quality fertilizers
        public override void OnCropRemovedAfterHarvest(HarvestContext context, int tier)
        {
            FertilizerHelper.TryUpgradeQualityFertilizer(context.Soil, tier);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Leaves {FertilizerHelper.GetQualityFertilizerName(tier)} in its soil once it's harvested for the last time";
        }

        public override string GetNotes()
        {
            return "Only upgrades soil with no fertilizer or a weaker quality fertilizer. Other fertilizers like Speed-Gro are left alone. A crop that grows back leaves it after its final harvest.";
        }
    }
}
