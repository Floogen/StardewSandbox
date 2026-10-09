using CropTraits.Framework.Models;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    public class RefreshingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Refreshing";

        public const int ENERGY_PER_TIER = 1;

        public override string Id => ID;

        // Junimos harvest without the player, so there's no one to refresh
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            if (context.JunimoHarvester is not null)
            {
                return;
            }

            Game1.player.Stamina = Math.Min(Game1.player.Stamina + (ENERGY_PER_TIER * tier), Game1.player.MaxStamina);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Restores {ENERGY_PER_TIER * tier} energy each time it's harvested";
        }

        public override string GetNotes()
        {
            return "Never goes past your maximum energy. Junimo harvests don't restore anything.";
        }
    }
}
