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
            return GetText("details", new { friendshipBonus = ToPercentValue(FRIENDSHIP_BONUS_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
