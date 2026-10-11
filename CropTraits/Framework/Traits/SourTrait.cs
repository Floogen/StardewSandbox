using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    public class SourTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Sour";

        public const string BUFF_ID = "PeacefulEnd.CropTraits_Sour";
        public const int SPEED_PENALTY = -1;
        public const int BUFF_MINUTES_PER_TIER = 30;
        public const int SPEED_BUFF_ICON_INDEX = 9;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;

        public override bool CanApplyTo(CropData cropData)
        {
            return CropDataHelper.HasEdibleHarvest(cropData);
        }

        public override Buff GetFoodBuff(int tier)
        {
            BuffEffects effects = new BuffEffects();
            effects.Speed.Value = SPEED_PENALTY;

            string traitName = CropTraits.traitManager.GetTraitName(ID);
            return new Buff(BUFF_ID, source: ID, displaySource: traitName, duration: BUFF_MINUTES_PER_TIER * tier * Game1.realMilliSecondsPerGameMinute, iconTexture: Game1.buffsIcons, iconSheetIndex: SPEED_BUFF_ICON_INDEX, effects: effects, displayName: traitName);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { speedPenalty = SPEED_PENALTY, minutes = (BUFF_MINUTES_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
