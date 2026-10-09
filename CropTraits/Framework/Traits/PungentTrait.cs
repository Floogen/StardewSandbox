namespace CropTraits.Framework.Traits
{
    public class PungentTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Pungent";

        public const double FRIENDSHIP_PENALTY_PER_TIER = 0.20;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;

        public override double ModifyGiftFriendship(double multiplier, int tier)
        {
            return multiplier * (1 - (FRIENDSHIP_PENALTY_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return $"Gives {ToPercent(FRIENDSHIP_PENALTY_PER_TIER * tier)} less friendship when given as a gift";
        }

        public override string GetNotes()
        {
            return "Only applies to gifts the villager doesn't dislike or hate, so it never makes a bad gift worse. Artisan goods made from it don't keep it.";
        }
    }
}
