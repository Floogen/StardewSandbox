using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    public class GildedTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Gilded";

        public const double GILDED_CHANCE = 0.5;
        public const double VALUE_MULTIPLIER = 2.0;

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 1;

        public override bool TryReplaceMainHarvest(HarvestContext context, Item item, int tier)
        {
            return TryTurnIntoCoins(item, GILDED_CHANCE);
        }

        // Shared with Fortunate. The sale price already includes quality, price traits like Sweet and professions like Tiller, so the doubling builds on all of them
        public static bool TryTurnIntoCoins(Item item, double chance)
        {
            if (item is not StardewValley.Object produce || Game1.random.NextDouble() >= chance)
            {
                return false;
            }

            int value = (int)Math.Round(produce.sellToStorePrice() * produce.Stack * VALUE_MULTIPLIER);
            if (value <= 0)
            {
                return false;
            }

            Game1.player.Money += value;
            Game1.playSound("sell");

            // Floats the amount over the player in gold, the same way damage numbers float over monsters
            GameLocation location = Game1.player.currentLocation;
            location?.debris.Add(new Debris(value, Game1.player.getStandingPosition(), Color.Gold, 1f, Game1.player));

            return true;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { gildedChance = ToPercentValue(GILDED_CHANCE), valueMultiplier = VALUE_MULTIPLIER.ToString("0.#") });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
