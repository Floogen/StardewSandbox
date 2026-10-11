using CropTraits.Framework.Models;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    // Fused from Refreshing and Exhausting, a neutral trade that pays off when the player is nearly out of energy
    public class SecondWindTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "SecondWind";

        public const int ENERGY_COST = 1;
        public const int ENERGY_RESTORED = 10;
        public const double LOW_ENERGY_SHARE = 0.20;
        public const float MIN_STAMINA = 0f;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => RefreshingTrait.ID;
        public override string SecondTraitId => ExhaustingTrait.ID;

        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            Farmer player = Game1.player;
            if (context.JunimoHarvester is not null)
            {
                return;
            }

            if (player.Stamina < player.MaxStamina * LOW_ENERGY_SHARE)
            {
                player.Stamina = Math.Min(player.Stamina + ENERGY_RESTORED, player.MaxStamina);
                return;
            }

            player.Stamina = Math.Max(player.Stamina - ENERGY_COST, MIN_STAMINA);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { energyCost = ENERGY_COST, energyRestored = ENERGY_RESTORED, lowEnergyShare = ToPercentValue(LOW_ENERGY_SHARE) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
