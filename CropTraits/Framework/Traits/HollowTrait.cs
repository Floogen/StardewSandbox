using CropTraits.Framework.Models;
using StardewValley;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class HollowTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Hollow";

        public const string FIBER_ID = "(O)771";
        public const double CRUMBLE_CHANCE_PER_TIER = 0.10;

        // Both turn the main harvest into something else, so only one can
        private static readonly string[] CONFLICTS = { GildedTrait.ID, WindfallTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // The fiber goes straight to the player, dropping at their feet when the inventory is full
        public override bool TryReplaceMainHarvest(HarvestContext context, Item item, int tier)
        {
            if (Game1.random.NextDouble() >= CRUMBLE_CHANCE_PER_TIER * tier)
            {
                return false;
            }

            Item fiber = ItemRegistry.Create(FIBER_ID);
            if (Game1.player.addItemToInventoryBool(fiber) is false)
            {
                Game1.createItemDebris(fiber, Game1.player.getStandingPosition(), -1, Game1.player.currentLocation);
            }

            Game1.playSound("leafrustle");
            return true;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { crumbleChance = ToPercentValue(CRUMBLE_CHANCE_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
