using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class GeodeRootsTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "GeodeRoots";

        // Each tier finds a deeper geode (Geode, Frozen Geode, then Magma Geode)
        public static readonly string[] GEODE_IDS_BY_TIER = { RootsHelper.GEODE_ID, RootsHelper.FROZEN_GEODE_ID, RootsHelper.MAGMA_GEODE_ID };
        public static readonly string[] GEODE_NAMES_BY_TIER = { "Geode", "Frozen Geode", "Magma Geode" };

        // Resprouting would let the crop grow back, while the ore roots keep a crop to one kind of bonus drop
        private static readonly string[] CONFLICTS = { CopperRootsTrait.ID, IronRootsTrait.ID, GoldRootsTrait.ID, IridiumRootsTrait.ID, ResproutingTrait.ID };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Geodes are a reward for replanting, so crops that grow back are left out
        public override bool CanApplyTo(CropData cropData)
        {
            return HarvestDropHelper.IsNonRenewable(cropData);
        }

        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            RootsHelper.DropGeode(context, GEODE_IDS_BY_TIER[GetTierIndex(tier)], xTile, yTile, location);
        }

        public static int GetTierIndex(int tier)
        {
            return Math.Clamp(tier, 1, GEODE_IDS_BY_TIER.Length) - 1;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Drops a {GEODE_NAMES_BY_TIER[GetTierIndex(tier)]} when harvested";
        }

        public override string GetNotes()
        {
            return RootsHelper.GetGeodeNotes();
        }
    }
}
