using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    // Fused from Contagious and Dominant, passing a trait to every neighbor of its type instead of just one
    public class PollinatingTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Pollinating";

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
            return GetText("details");
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
