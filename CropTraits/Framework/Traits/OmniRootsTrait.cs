using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Geode Roots and Variegated, since an Omni Geode holds a mix of everything
    public class OmniRootsTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "OmniRoots";

        // Resprouting would let the crop grow back, while the other roots keep a crop to one kind of bonus drop
        private static readonly string[] CONFLICTS = { GeodeRootsTrait.ID, CopperRootsTrait.ID, IronRootsTrait.ID, GoldRootsTrait.ID, IridiumRootsTrait.ID, ResproutingTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => GeodeRootsTrait.ID;
        public override string SecondTraitId => VariegatedTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Geodes are a reward for replanting, so crops that grow back are left out
        public override bool CanApplyTo(CropData cropData)
        {
            return HarvestDropHelper.IsNonRenewable(cropData);
        }

        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            RootsHelper.DropGeode(context, RootsHelper.OMNI_GEODE_ID, xTile, yTile, location);
        }

        public override string GetTierDetails(int tier)
        {
            return "Drops an Omni Geode when harvested";
        }

        public override string GetNotes()
        {
            return RootsHelper.GetGeodeNotes();
        }
    }
}
