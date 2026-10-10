using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    // One of the food buff traits, along with Lucky and Earthy
    public class BrinyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Briny";

        public const string BUFF_ID = "PeacefulEnd.CropTraits_Briny";
        public const int SKILL_BONUS = 1;
        public const int BUFF_MINUTES_PER_TIER = 120;
        public const int BUFF_ICON_INDEX = 1;

        public override string Id => ID;

        public override bool CanApplyTo(CropData cropData)
        {
            return CropDataHelper.HasEdibleHarvest(cropData);
        }

        public override Buff GetFoodBuff(int tier)
        {
            BuffEffects effects = new BuffEffects();
            effects.FishingLevel.Value = SKILL_BONUS;

            return FoodHelper.CreateTraitBuff(ID, BUFF_ID, effects, BUFF_MINUTES_PER_TIER * tier, BUFF_ICON_INDEX);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Eating it gives +{SKILL_BONUS} Fishing for {BUFF_MINUTES_PER_TIER * tier / 60} hours";
        }

        public override string GetNotes()
        {
            return "Eating more refreshes the buff instead of stacking it. Only appears on crops with edible produce.";
        }
    }
}
