using StardewValley;

namespace CropTraits.Framework.Traits
{
    // Fused from Charming and Lucky, a lucky charm no villager can turn down
    public class CharmedTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Charmed";

        public const double FRIENDSHIP_BONUS = 0.50;

        public override string Id => ID;
        public override string FirstTraitId => CharmingTrait.ID;
        public override string SecondTraitId => LuckyTrait.ID;

        public override int ModifyGiftTaste(int giftTaste, int tier)
        {
            return giftTaste == NPC.gift_taste_dislike || giftTaste == NPC.gift_taste_hate ? NPC.gift_taste_neutral : giftTaste;
        }

        public override double ModifyGiftFriendship(double multiplier, int tier)
        {
            return multiplier * (1 + FRIENDSHIP_BONUS);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { friendshipBonus = ToPercentValue(FRIENDSHIP_BONUS) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
