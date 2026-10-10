using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    // Thorny keeps crows off, while Prickly hurts whoever picks it
    public class PricklyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Prickly";

        public const int DAMAGE_PER_TIER = 2;
        public const int MIN_HEALTH = 1;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;

        // Never takes the player below 1 health, so harvesting alone can't knock them out
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            Farmer player = Game1.player;
            if (context.JunimoHarvester is not null || player.health <= MIN_HEALTH)
            {
                return;
            }

            int damage = Math.Min(DAMAGE_PER_TIER * tier, player.health - MIN_HEALTH);
            player.health -= damage;
            player.currentLocation?.debris.Add(new Debris(damage, player.getStandingPosition(), Color.Red, 1f, player));
            Game1.playSound("ow");
        }

        public override string GetTierDetails(int tier)
        {
            return $"Picking it costs {DAMAGE_PER_TIER * tier} health";
        }

        public override string GetNotes()
        {
            return "Never takes you below 1 health. Junimos don't mind the prickles.";
        }
    }
}
