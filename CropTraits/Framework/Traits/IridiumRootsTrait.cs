using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Gold Roots and Deep Rooted, the last step of the ore roots
    public class IridiumRootsTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "IridiumRoots";

        public const int MAX_ORE_COUNT = 1;

        // Resprouting would let the crop grow back, while the lower roots can't stack with this one
        private static readonly string[] CONFLICTS = { CopperRootsTrait.ID, IronRootsTrait.ID, GoldRootsTrait.ID, ResproutingTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => GoldRootsTrait.ID;
        public override string SecondTraitId => DeepRootedTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Ore is a reward for replanting, so crops that grow back are left out
        public override bool CanApplyTo(CropData cropData)
        {
            return HarvestDropHelper.IsNonRenewable(cropData);
        }

        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            RootsHelper.DropOre(context, RootsHelper.IRIDIUM_ORE_ID, MAX_ORE_COUNT, xTile, yTile, location);
        }

        public override string GetTierDetails(int tier)
        {
            return RootsHelper.GetOreRangeText(MAX_ORE_COUNT, "iridium ore");
        }

        public override string GetNotes()
        {
            return RootsHelper.GetOreNotes();
        }
    }
}
