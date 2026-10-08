using CropTraits.Framework.Models;

namespace CropTraits.Framework.Traits
{
    // Fused from Drought Tolerant and Rain Loving
    public class DeepRootedTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "DeepRooted";

        public override string Id => ID;
        public override string FirstTraitId => DroughtTolerantTrait.ID;
        public override string SecondTraitId => RainLovingTrait.ID;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            context.Plan.CountsAsWatered = true;
        }

        public override string GetTierDetails(int tier)
        {
            return "Never needs watering";
        }

        public override string GetNotes()
        {
            return "The soil still looks dry.";
        }
    }
}
