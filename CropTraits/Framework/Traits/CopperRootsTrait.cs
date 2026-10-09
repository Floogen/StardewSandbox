using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class CopperRootsTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "CopperRoots";

        public const int MAX_ORE_COUNT = 5;

        // Resprouting would let the crop grow back
        private static readonly string[] CONFLICTS = { ResproutingTrait.ID };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Ore is a reward for replanting, so crops that grow back are left out
        public override bool CanApplyTo(CropData cropData)
        {
            return HarvestDropHelper.IsNonRenewable(cropData);
        }

        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            RootsHelper.DropOre(context, RootsHelper.COPPER_ORE_ID, MAX_ORE_COUNT, xTile, yTile, location);
        }

        public override string GetTierDetails(int tier)
        {
            return RootsHelper.GetOreRangeText(MAX_ORE_COUNT, "copper ore");
        }

        public override string GetNotes()
        {
            return RootsHelper.GetOreNotes();
        }
    }
}
