using CropTraits.Framework.Models;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class ThirstyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Thirsty";

        private static readonly string[] CONFLICTS = { DroughtTolerantTrait.ID, DeepRootedTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override bool CanApplyTo(CropData cropData)
        {
            return cropData?.NeedsWatering ?? false;
        }

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            context.Plan.LosesGrowthWhenDry = true;
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
