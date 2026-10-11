using CropTraits.Framework.Models;
using StardewValley;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Refreshing
    public class ExhaustingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Exhausting";

        public const int ENERGY_PER_TIER = 1;
        public const float MIN_STAMINA = 0f;

        private static readonly string[] CONFLICTS = { RefreshingTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Stops at zero so harvesting alone can never make the player pass out
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            if (context.JunimoHarvester is not null || Game1.player.Stamina <= MIN_STAMINA)
            {
                return;
            }

            Game1.player.Stamina = Math.Max(Game1.player.Stamina - (ENERGY_PER_TIER * tier), MIN_STAMINA);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { energy = (ENERGY_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
