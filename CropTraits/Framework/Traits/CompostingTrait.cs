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
            return GetText("details", new { fertilizer = FertilizerHelper.GetQualityFertilizerName(tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
