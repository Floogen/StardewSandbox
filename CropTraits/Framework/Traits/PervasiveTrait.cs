using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    // Fused from Contagious and Dominant, passing a trait to every neighbor of its type instead of just one
    public class PervasiveTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Pervasive";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => ContagiousTrait.ID;
        public override string SecondTraitId => DominantTrait.ID;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            ContagiousTrait.SpreadTraits(context, true);
        }

        public override string GetTierDetails(int tier)
        {
            return "Once ready to harvest, copies one of its traits (at tier I) onto every adjacent crop of the same type with room";
        }

        public override string GetNotes()
        {
            return "Covers all eight surrounding tiles. Each neighbor gets its own random pick. Spreads once per planting, even alongside Contagious. Never passes on itself or combination traits.";
        }
    }
}
