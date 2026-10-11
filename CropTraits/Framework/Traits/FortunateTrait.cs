using CropTraits.Framework.Models;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    // Fused from Fickle and Gilded, letting the day's luck decide how often the harvest turns into coins
    public class FortunateTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Fortunate";

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
            return $"Has a {ToPercent(MIN_CHANCE)} to {ToPercent(MAX_CHANCE)} chance to turn the main harvest into coins worth {GildedTrait.VALUE_MULTIPLIER:0.#}x its sale price, higher on luckier days";
        }

        public override string GetNotes()
        {
            return $"A neutral luck day gives {ToPercent(BASE_CHANCE)}, above Gilded's usual {ToPercent(GildedTrait.GILDED_CHANCE)}. A crop that also has Gilded rolls Fortunate first, then Gilded if that fails. Extra produce is never turned into coins. Junimo harvests are never turned into coins. The coins don't count as shipped.";
        }
    }
}
