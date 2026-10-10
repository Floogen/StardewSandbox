using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.Buffs;

namespace CropTraits.Framework.Traits
{
    // One of the food buff traits, along with Briny and Earthy
    public class LuckyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Lucky";

        public const string BUFF_ID = "PeacefulEnd.CropTraits_Lucky";
        public const int SKILL_BONUS = 1;
        public const int BUFF_MINUTES_PER_TIER = 120;
        public const int BUFF_ICON_INDEX = 4;

        public override string Id => ID;

        public override Buff GetFoodBuff(int tier)
        {
            BuffEffects effects = new BuffEffects();
            effects.LuckLevel.Value = SKILL_BONUS;

            return FoodHelper.CreateTraitBuff(ID, BUFF_ID, effects, BUFF_MINUTES_PER_TIER * tier, BUFF_ICON_INDEX);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Eating it gives +{SKILL_BONUS} Luck for {BUFF_MINUTES_PER_TIER * tier / 60} hours";
        }

        public override string GetNotes()
        {
            return "Eating more refreshes the buff instead of stacking it.";
        }
    }
}
