using CropTraits.Framework.Models;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Composting
    public class DepletingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Depleting";

        private static readonly string[] CONFLICTS = { CompostingTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override void OnCropRemovedAfterHarvest(HarvestContext context, int tier)
        {
            if (context.Soil is not null && string.IsNullOrEmpty(context.Soil.fertilizer.Value) is false)
            {
                context.Soil.fertilizer.Value = null;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details");
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
