namespace CropTraits.Framework.Traits
{
    // Fused from Thorny and Invasive, turning a harmful trait into a tiny scarecrow
    public class GuardianTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Guardian";

        public override string Id => ID;
        public override string FirstTraitId => ThornyTrait.ID;
        public override string SecondTraitId => InvasiveTrait.ID;
        public override bool IsIgnoredByCrows => true;
        public override bool ProtectsNeighborsFromCrows => true;

        public override string GetTierDetails(int tier)
        {
            return "Crows never eat it. It also protects the eight tiles around it like a tiny scarecrow";
        }

        public override string GetNotes()
        {
            return "Crops with Crow Magnet aren't protected.";
        }
    }
}
