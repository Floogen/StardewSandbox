namespace CropTraits.Framework.Traits
{
    // Only protects against mutation, so dilution can still remove traits (including Stable itself)
    public class StableTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Stable";

        public override string Id => ID;
        public override int MaxTier => 1;
        public override int Weight => 3;
        public override bool BlocksMutationReplacement => true;
    }
}
