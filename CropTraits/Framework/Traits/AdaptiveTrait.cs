namespace CropTraits.Framework.Traits
{
    // Fused from Hardy and Resprouting, giving up the regrowths to grow in every season
    public class AdaptiveTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Adaptive";

        public override string Id => ID;
        public override string FirstTraitId => HardyTrait.ID;
        public override string SecondTraitId => ResproutingTrait.ID;
        public override bool GrowsInEverySeason => true;

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
