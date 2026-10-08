using StardewValley.GameData.Crops;
using System.Linq;

namespace CropTraits.Framework.Traits
{
    public class HardyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Hardy";

        public const int NIGHTS_PER_TIER = 3;
        public const int SEASON_COUNT = 4;

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Uncommon;

        // Crops that already grow in every season never reach the end of one
        public override bool CanApplyTo(CropData cropData)
        {
            return cropData?.Seasons is not null && cropData.Seasons.Distinct().Count() < SEASON_COUNT;
        }

        public override int GetOutOfSeasonNights(int tier)
        {
            return NIGHTS_PER_TIER * tier;
        }

        public override string GetTierDetails(int tier)
        {
            return $"Keeps growing for {NIGHTS_PER_TIER * tier} days after its season ends";
        }

        public override string GetNotes()
        {
            return "Only matters outdoors, where crops die when their season ends. Once the extra days run out, the crop dies as normal.";
        }
    }
}
