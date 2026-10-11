using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.Buffs;

namespace CropTraits.Framework.Traits
{
    // Fused from Sour and Sweet, keeping Sweet's price bonus and turning Sour's slowdown around
    public class TangyTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Tangy";

        public const double PRICE_BONUS = 0.20;
        public const string BUFF_ID = "PeacefulEnd.CropTraits_Tangy";
        public const int SPEED_BONUS = 1;
        public const int BUFF_MINUTES = 60;
        public const int SPEED_BUFF_ICON_INDEX = 9;

        public override string Id => ID;
        public override string FirstTraitId => SourTrait.ID;
        public override string SecondTraitId => SweetTrait.ID;
        public override bool IsPriceTrait => true;

        public override double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier * (1 + PRICE_BONUS);
        }

        public override Buff GetFoodBuff(int tier)
        {
            BuffEffects effects = new BuffEffects();
            effects.Speed.Value = SPEED_BONUS;

            return FoodHelper.CreateTraitBuff(ID, BUFF_ID, effects, BUFF_MINUTES, SPEED_BUFF_ICON_INDEX);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { priceBonus = ToPercentValue(PRICE_BONUS), speedBonus = SPEED_BONUS, hours = (BUFF_MINUTES / 60) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
