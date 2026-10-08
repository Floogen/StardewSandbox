namespace CropTraits.Framework.Traits
{
    // Fused from Stable and Resilient, keeping Stable's protection from mutations
    public class HeirloomTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Heirloom";

        public override string Id => ID;
        public override string FirstTraitId => StableTrait.ID;
        public override string SecondTraitId => ResilientTrait.ID;
        public override bool AlwaysPassesOnTraits => true;
        public override bool BlocksMutationReplacement => true;

        public override string GetTierDetails(int tier)
        {
            return "Its traits always carry over when crossbred. Mutations can't replace them either";
        }

        public override string GetNotes()
        {
            return "The three trait limit and conflicts still apply when crossbreeding.";
        }
    }
}
