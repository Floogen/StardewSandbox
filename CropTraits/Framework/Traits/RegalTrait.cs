namespace CropTraits.Framework.Traits
{
    // Acts like extra fertilizer quality levels, so Regal III alone matches Deluxe Fertilizer and allows iridium quality
    public class RegalTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Regal";

        public const int QUALITY_LEVEL_PER_TIER = 1;

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Rare;

        public override int ModifyQualityBoostLevel(int level, int tier)
        {
            return level + (QUALITY_LEVEL_PER_TIER * tier);
        }

        public override string GetTierDetails(int tier)
        {
            int levels = QUALITY_LEVEL_PER_TIER * tier;
            return levels == 1 ? "Quality odds raised by 1 fertilizer level" : $"Quality odds raised by {levels} fertilizer levels";
        }

        public override string GetNotes()
        {
            return "Each level matches one step of fertilizer (Basic, Quality then Deluxe) and stacks with any fertilizer used.";
        }
    }
}
