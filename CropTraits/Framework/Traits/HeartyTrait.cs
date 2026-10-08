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

            string traitName = CropTraits.traitManager.GetTraitName(ID);
            return new Buff(BUFF_ID, source: ID, displaySource: traitName, duration: BUFF_MINUTES * Game1.realMilliSecondsPerGameMinute, iconTexture: Game1.buffsIcons, iconSheetIndex: FARMING_BUFF_ICON_INDEX, effects: effects, displayName: traitName);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Restores {ToPercent(RESTORATION_BONUS)} more energy and health when eaten, plus +{FARMING_BONUS} Farming and +{SPEED_BONUS} Speed for {BUFF_MINUTES / 60} hours";
        }

        public override string GetNotes()
        {
            return "Eating more refreshes the buff instead of stacking it.";
        }
    }
}
