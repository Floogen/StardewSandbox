namespace CropTraits.Framework.Traits
{
    // Fused from Charming and Pungent, a neutral gamble that makes every reaction stronger
    public class AcquiredTasteTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "AcquiredTaste";

        public const double REACTION_MULTIPLIER = 2.0;

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
            return $"Every gift reaction is {REACTION_MULTIPLIER:0.#}x as strong, good or bad";
        }

        public override string GetNotes()
        {
            return "Loved and liked gifts give twice the friendship, while disliked and hated gifts lose twice as much. Artisan goods made from it don't keep it.";
        }
    }
}
