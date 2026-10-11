namespace CropTraits.Framework.Traits
{
    // Fused from Charming and Pungent, a neutral gamble that makes every reaction stronger
    public class AcquiredTasteTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "AcquiredTaste";

        public const double REACTION_MULTIPLIER = 1.5;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => CharmingTrait.ID;
        public override string SecondTraitId => PungentTrait.ID;

        public override double ModifyGiftFriendship(double multiplier, int tier)
        {
            return multiplier * REACTION_MULTIPLIER;
        }

        public override double ModifyBadGiftFriendship(double multiplier, int tier)
        {
            return multiplier * REACTION_MULTIPLIER;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { reactionMultiplier = REACTION_MULTIPLIER.ToString("0.#") });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { reactionMultiplier = REACTION_MULTIPLIER.ToString("0.#") });
        }
    }
}
