namespace CropTraits.Framework.Traits
{
    // Acts like extra fertilizer quality levels, so Regal III alone matches Deluxe Fertilizer and allows iridium quality
    public class RegalTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Regal";

        public const int QUALITY_LEVEL_PER_TIER = 1;

        public override string Id => ID;
        public override int Weight => 6;

        public override int ModifyQualityBoostLevel(int level, int tier)
        {
            return level + (QUALITY_LEVEL_PER_TIER * tier);
        }
    }
}
