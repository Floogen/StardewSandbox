using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    // Fused from Bitter and Sweet, turning two opposite price traits into a daily gamble
    public class BittersweetTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Bittersweet";

        public const double HIGH_PRICE_BONUS = 0.40;
        public const double LOW_PRICE_PENALTY = 0.20;
        public const double HIGH_PRICE_CHANCE = 0.5;
        public const int PRICE_RANDOM_SEED = 7333; // Keeps the daily price roll apart from other rolls made on the same day

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => BitterTrait.ID;
        public override string SecondTraitId => SweetTrait.ID;
        public override bool IsPriceTrait => true;

        // Seeded by day and save only, so every Bittersweet item shares the same price that day
        public override double ModifySellPrice(double multiplier, int tier)
        {
            Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, PRICE_RANDOM_SEED);
            return multiplier * (random.NextDouble() < HIGH_PRICE_CHANCE ? 1 + HIGH_PRICE_BONUS : 1 - LOW_PRICE_PENALTY);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Each day, sells for either {ToPercent(HIGH_PRICE_BONUS)} more or {ToPercent(LOW_PRICE_PENALTY)} less";
        }

        public override string GetNotes()
        {
            return "Every Bittersweet item has the same price on a given day. The change also carries into artisan goods.";
        }
    }
}
