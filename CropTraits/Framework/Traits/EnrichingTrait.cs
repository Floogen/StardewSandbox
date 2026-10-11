using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Traits
{
    // Fused from Composting and Nurturing, sharing its fertilizer with the empty soil around it
    public class EnrichingTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Enriching";

        public const int FERTILIZER_RANK = 2;

        public override string Id => ID;
        public override string FirstTraitId => CompostingTrait.ID;
        public override string SecondTraitId => NurturingTrait.ID;

        // The neighbors are only reached from ground soil, so a garden pot only enriches itself
        public override void OnCropRemovedAfterHarvest(HarvestContext context, int tier)
        {
            HoeDirt soil = context.Soil;
            if (soil is null)
            {
                return;
            }

            FertilizerHelper.TryUpgradeQualityFertilizer(soil, FERTILIZER_RANK);

            GameLocation location = soil.Location;
            Vector2 tile = soil.Tile;
            if (location is null || location.terrainFeatures.TryGetValue(tile, out TerrainFeature ownFeature) is false || ownFeature != soil)
            {
                return;
            }

            foreach (Vector2 neighborTile in Utility.getSurroundingTileLocationsArray(tile))
            {
                if (location.terrainFeatures.TryGetValue(neighborTile, out TerrainFeature feature) && feature is HoeDirt neighborDirt && neighborDirt.crop is null)
                {
                    FertilizerHelper.TryUpgradeQualityFertilizer(neighborDirt, FERTILIZER_RANK);
                }
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { fertilizer = FertilizerHelper.GetQualityFertilizerName(FERTILIZER_RANK) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
