using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    // One of the food buff traits, along with Lucky and Briny
    public class EarthyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Earthy";

        public const string BUFF_ID = "PeacefulEnd.CropTraits_Earthy";
        public const int SKILL_BONUS = 1;
        public const int BUFF_MINUTES_PER_TIER = 120;
        public const int BUFF_ICON_INDEX = 2;

        public override string Id => ID;

        public override bool CanApplyTo(CropData cropData)
        {
            return CropDataHelper.HasEdibleHarvest(cropData);
        }

        public override Buff GetFoodBuff(int tier)
        {
            BuffEffects effects = new BuffEffects();
            effects.MiningLevel.Value = SKILL_BONUS;

            return FoodHelper.CreateTraitBuff(ID, BUFF_ID, effects, BUFF_MINUTES_PER_TIER * tier, BUFF_ICON_INDEX);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { skillBonus = SKILL_BONUS, hours = (BUFF_MINUTES_PER_TIER * tier / 60) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
