using CropTraits.Framework.Models;
using StardewValley.GameData.Crops;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Traits
{
    public class DroughtTolerantTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "DroughtTolerant";

        public const double WATERED_CHANCE_PER_TIER = 0.25;

        public override string Id => ID;

        public override bool CanApplyTo(CropData cropData)
        {
            return cropData?.NeedsWatering ?? false;
        }

        // Rolled in the morning so the player can see the wet soil, letting the game's own watering check grow the crop that night
        public override void OnDayStarted(MorningContext context, int tier)
        {
            if (context.Dirt.state.Value == HoeDirt.watered)
            {
                return;
            }

            if (context.Random.NextDouble() < WATERED_CHANCE_PER_TIER * tier)
            {
                context.Dirt.state.Value = HoeDirt.watered;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { wateredChance = ToPercentValue(WATERED_CHANCE_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
