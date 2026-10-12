using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Traits
{
    // Fused from Drought Tolerant and Rain Loving
    public class DeepRootedTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "DeepRooted";

        public override string Id => ID;
        public override string FirstTraitId => DroughtTolerantTrait.ID;
        public override string SecondTraitId => RainLovingTrait.ID;

        public override bool CountsAsWatered(GameLocation location, Vector2 tile, int tier)
        {
            return true;
        }

        // Waters its soil so the player can see it never needs watering
        public override void OnDayStarted(MorningContext context, int tier)
        {
            if (context.Dirt.state.Value != HoeDirt.watered)
            {
                context.Dirt.state.Value = HoeDirt.watered;
            }
        }

        // Kept so a crop planted during the day, after the morning watering, still grows that night
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            context.Plan.CountsAsWatered = CountsAsWatered(context.Location, context.Tile, tier);
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
