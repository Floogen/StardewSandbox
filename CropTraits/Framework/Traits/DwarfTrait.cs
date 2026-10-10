using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Stunted and Small, keeping the small look but growing faster and selling at full price
    public class DwarfTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Dwarf";

        public const float SIZE_REDUCTION = 0.25f;
        public const double SPEED_BONUS = 0.25;

        // Large would undo its size, while Colossal and Titanic only matter on giant crops it can't become
        private static readonly string[] CONFLICTS = { LargeTrait.ID, ColossalTrait.ID, TitanicTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => StuntedTrait.ID;
        public override string SecondTraitId => SmallTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool PreventsGiantCrop => true;

        public override float ModifyProduceScale(float scale, int tier)
        {
            return scale * (1 - SIZE_REDUCTION);
        }

        // Removes a share of the growth days like Swift, with each phase keeping at least one day
        public override void ModifyGrowth(IList<int> phaseDays, CropData cropData, int tier)
        {
            GrowthHelper.RemoveGrowthDays(phaseDays, (int)Math.Ceiling(GrowthHelper.GetGrowthDays(phaseDays) * SPEED_BONUS));
        }

        public override string GetTierDetails(int tier)
        {
            return $"{ToPercent(SIZE_REDUCTION)} smaller with {ToPercent(SPEED_BONUS)} shorter growth time. Still sells at full price";
        }

        public override string GetNotes()
        {
            return "Can never become a giant crop. Growth time is rounded up to whole days, with each growth stage keeping at least one day.";
        }
    }
}
