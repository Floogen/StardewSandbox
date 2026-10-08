using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class CrowHelper
    {
        // Called from Farm.addCrows in place of the game's scarecrow result, so returning true keeps the crop from being eaten
        public static bool IsSafeFromCrow(bool isProtectedByScarecrow, HoeDirt dirt, Vector2 tile, GameLocation location)
        {
            TraitSet traits = TraitModDataHelper.GetTraits(dirt?.modData);
            if (CropTraits.traitManager.IsIgnoredByCrows(traits))
            {
                return true;
            }

            // Traits like Crow Magnet ignore scarecrows and protective neighbors alike
            if (CropTraits.traitManager.IgnoresScarecrows(traits))
            {
                return false;
            }

            return isProtectedByScarecrow || IsGuardedByNeighbor(location, tile);
        }

        public static bool IsGuardedByNeighbor(GameLocation location, Vector2 tile)
        {
            foreach (Vector2 neighborTile in Utility.getSurroundingTileLocationsArray(tile))
            {
                if (PlantedTraitHelper.TryGetDirt(location, neighborTile, out HoeDirt neighborDirt) is false || neighborDirt.crop is null || neighborDirt.crop.dead.Value)
                {
                    continue;
                }

                if (CropTraits.traitManager.ProtectsNeighborsFromCrows(TraitModDataHelper.GetTraits(neighborDirt.modData)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
