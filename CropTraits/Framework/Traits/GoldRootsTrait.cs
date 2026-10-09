using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Iron Roots and Conductive
    public class GoldRootsTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "GoldRoots";

        public const int MAX_ORE_COUNT = 2;

        // Resprouting would let the crop grow back, while the lower roots can't stack with this one
        private static readonly string[] CONFLICTS = { CopperRootsTrait.ID, IronRootsTrait.ID, ResproutingTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => IronRootsTrait.ID;
        public override string SecondTraitId => ConductiveTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Ore is a reward for replanting, so crops that grow back are left out
        public override bool CanApplyTo(CropData cropData)
        {
            return HarvestDropHelper.IsNonRenewable(cropData);
        }

        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            RootsHelper.DropOre(context, RootsHelper.GOLD_ORE_ID, MAX_ORE_COUNT, xTile, yTile, location);
        }

        public override string GetTierDetails(int tier)
        {
            return RootsHelper.GetOreRangeText(MAX_ORE_COUNT, "gold ore");
        }

        public override string GetNotes()
        {
            return RootsHelper.GetOreNotes();
        }
    }
}
