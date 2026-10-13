using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Small and Prolific, like cherry tomatoes: smaller produce, but always one more
    public class ClusteredTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Clustered";

        public const float SIZE_REDUCTION = 0.25f;
        public const double PRICE_PENALTY = 0.35;
        public const int EXTRA_ITEMS = 1;

        private static readonly string[] CONFLICTS = { LargeTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => SmallTrait.ID;
        public override string SecondTraitId => ProlificTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool IsPriceTrait => true;

        public override float ModifyProduceScale(float scale, int tier)
        {
            return scale * (1 - SIZE_REDUCTION);
        }

        public override double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier * (1 - PRICE_PENALTY);
        }

        // A copy of the main harvest keeps its quality and traits, the same way Prolific's extra harvest does
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            for (int extraItem = 0; extraItem < EXTRA_ITEMS; extraItem++)
            {
                Item extraProduce = context.HarvestedItem?.getOne() ?? ItemRegistry.Create(context.QualifiedHarvestItemId);
                HarvestDropHelper.Drop(context, extraProduce, xTile, yTile, location);
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { sizeReduction = ToPercentValue(SIZE_REDUCTION), pricePenalty = ToPercentValue(PRICE_PENALTY), extraItems = EXTRA_ITEMS });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
