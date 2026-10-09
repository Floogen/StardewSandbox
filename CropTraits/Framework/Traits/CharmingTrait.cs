using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class CharmingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Charming";

        public const double FRIENDSHIP_BONUS_PER_TIER = 0.20;

        private static readonly string[] CONFLICTS = { PungentTrait.ID };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override double ModifyGiftFriendship(double multiplier, int tier)
        {
            return multiplier * (1 + (FRIENDSHIP_BONUS_PER_TIER * tier));
        }

        public override string GetTierDetails(int tier)
        {
            return $"Gives {ToPercent(FRIENDSHIP_BONUS_PER_TIER * tier)} more friendship when given as a gift";
        }

        public override string GetNotes()
        {
            return "Only applies to gifts the villager doesn't dislike or hate. Artisan goods made from it don't keep it.";
        }
    }
}
