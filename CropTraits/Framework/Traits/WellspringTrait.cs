using CropTraits.Framework.Models;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Traits
{
    // Fused from Thirsty and Dewy, turning a crop that needs water into one that gives it
    public class WellspringTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Wellspring";

        public override string Id => ID;
        public override string FirstTraitId => ThirstyTrait.ID;
        public override string SecondTraitId => DewyTrait.ID;

        // Its own soil is watered even in a garden pot, while the soil around it is only reached on the ground
        public override void OnDayStarted(MorningContext context, int tier)
        {
            if (context.Dirt.state.Value != HoeDirt.watered)
            {
                context.Dirt.state.Value = HoeDirt.watered;
            }

            DewyTrait.WaterTiles(context, DewyTrait.SIDE_OFFSETS, false);
            DewyTrait.WaterTiles(context, DewyTrait.CORNER_OFFSETS, false);
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
