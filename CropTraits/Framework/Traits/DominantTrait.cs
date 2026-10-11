namespace CropTraits.Framework.Traits
{
    // Only biases which trait a neighbor rolls, so the neighbor's mutation chance is never changed. Raises the cap rather than the pull, so it matters most around higher tier traits
    public class DominantTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Dominant";

        public const int BONUS_CAP_MULTIPLIER = 2;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 1;

        public override int GetNeighborBonusCapMultiplier(int tier)
        {
            return BONUS_CAP_MULTIPLIER;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Its traits can pull up to {BONUS_CAP_MULTIPLIER} times as far on which trait nearby crops of the same type mutate into";
        }

        public override string GetNotes()
        {
            return "Covers all eight surrounding tiles. Doesn't change how likely they are to mutate. Each neighbor still pulls as hard as normal, so the higher limit matters most when the nearby crops hold higher tier traits.";
        }
    }
}
