using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    public class TenderTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Tender";

        public const double FLATTEN_CHANCE = 0.10;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;

        // Uses the weather of the day that's ending, so a stormy day can flatten it overnight
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors is false || context.Location.IsLightningHere() is false)
            {
                return;
            }

            if (context.Random.NextDouble() < FLATTEN_CHANCE)
            {
                context.Plan.IsFlattened = true;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(FLATTEN_CHANCE)} chance to die overnight after a stormy day";
        }

        public override string GetNotes()
        {
            return "Only applies outdoors. Rain without lightning doesn't count.";
        }
    }
}
