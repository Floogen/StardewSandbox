using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    // Fused from Nocturnal and Luminous, glowing at night and at its best when picked then
    public class MoonbloomTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Moonbloom";

        public override string Id => ID;
        public override string FirstTraitId => NocturnalTrait.ID;
        public override string SecondTraitId => LuminousTrait.ID;
        public override bool GlowsAtNight => true;

        public override int ModifyHarvestQuality(int quality, Random random, int tier)
        {
            return Game1.timeOfDay >= NocturnalTrait.NIGHT_START_TIME ? QualityHelper.ShiftQuality(quality, 1) : quality;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details");
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
