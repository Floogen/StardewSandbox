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
        public const float MAX_STAMINA_SHARE = 0.75f;

        public override string Id => ID;

        // Junimos harvest without the player, so there's no one to refresh. Stops at the cap without taking away energy already above it
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            float staminaCap = GetStaminaCap(Game1.player);
            if (context.JunimoHarvester is not null || Game1.player.Stamina >= staminaCap)
            {
                return;
            }

            Game1.player.Stamina = Math.Min(Game1.player.Stamina + (ENERGY_PER_TIER * tier), staminaCap);
        }

        // Keeps a large field from refilling the whole energy bar, so food still has a place
        public static float GetStaminaCap(Farmer player)
        {
            return player.MaxStamina * MAX_STAMINA_SHARE;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Restores {ENERGY_PER_TIER * tier} energy each time it's harvested";
        }

        public override string GetNotes()
        {
            return $"Only restores up to {ToPercent(MAX_STAMINA_SHARE)} of your maximum energy. Junimo harvests don't restore anything.";
        }
    }
}
