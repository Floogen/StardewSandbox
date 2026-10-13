using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.Buffs;

namespace CropTraits.Framework.Traits
{
    // Fused from Nourishing and Regal, keeping a Nourishing II boost and adding a short buff
    public class HeartyTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Hearty";

        public const string BUFF_ID = "PeacefulEnd.CropTraits_Hearty";
        public const double RESTORATION_BONUS = 0.50;
        public const int FARMING_BONUS = 1;
        public const int SPEED_BONUS = 1;
        public const int BUFF_MINUTES = 180;
        public const int FARMING_BUFF_ICON_INDEX = 0;

        public override string Id => ID;
        public override string FirstTraitId => NourishingTrait.ID;
        public override string SecondTraitId => RegalTrait.ID;

        public override double ModifyFoodRestoration(double multiplier, int tier)
        {
            return multiplier * (1 + RESTORATION_BONUS);
        }

        public override Buff GetFoodBuff(int tier)
        {
            BuffEffects effects = new BuffEffects();
            effects.FarmingLevel.Value = FARMING_BONUS;
            effects.Speed.Value = SPEED_BONUS;

            return FoodHelper.CreateTraitBuff(ID, BUFF_ID, effects, BUFF_MINUTES, FARMING_BUFF_ICON_INDEX);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { restorationBonus = ToPercentValue(RESTORATION_BONUS), farmingBonus = FARMING_BONUS, speedBonus = SPEED_BONUS, hours = (BUFF_MINUTES / 60) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
