using CropTraits.Framework.Models;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    // Fused from Fickle and Gilded, letting the day's luck decide how often the harvest turns into coins
    public class WindfallTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Windfall";

        public const double BASE_CHANCE = 0.60;
        public const double CHANCE_PER_LUCK = 2.5; // Daily luck usually sits between -0.1 and 0.1, so this spans the full range below
        public const double MIN_CHANCE = 0.35;
        public const double MAX_CHANCE = 0.85;

        public override string Id => ID;
        public override string FirstTraitId => FickleTrait.ID;
        public override string SecondTraitId => GildedTrait.ID;

        public override bool TryReplaceMainHarvest(HarvestContext context, Item item, int tier)
        {
            return GildedTrait.TryTurnIntoCoins(item, GetChance(Game1.player.DailyLuck));
        }

        public static double GetChance(double dailyLuck)
        {
            return Math.Clamp(BASE_CHANCE + (dailyLuck * CHANCE_PER_LUCK), MIN_CHANCE, MAX_CHANCE);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { minChance = ToPercentValue(MIN_CHANCE), maxChance = ToPercentValue(MAX_CHANCE), valueMultiplier = GildedTrait.VALUE_MULTIPLIER.ToString("0.#") });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { baseChance = ToPercentValue(BASE_CHANCE), gildedChance = ToPercentValue(GildedTrait.GILDED_CHANCE) });
        }
    }
}
