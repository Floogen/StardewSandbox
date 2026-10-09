namespace CropTraits.Framework.Traits
{
    // Only biases which trait a neighbor rolls, so the neighbor's mutation chance is never changed
    public class DominantTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Dominant";

        public const int INFLUENCE_MULTIPLIER = 2;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 1;

        public override int GetNeighborInfluenceMultiplier(int tier)
        {
            return INFLUENCE_MULTIPLIER;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Its traits pull {INFLUENCE_MULTIPLIER} times as hard on which trait nearby crops of the same type mutate into";
        }

        public override string GetNotes()
        {
            return "Covers all eight surrounding tiles. Doesn't change how likely they are to mutate, and each trait's pull still has the usual limit.";
        }
    }
}
