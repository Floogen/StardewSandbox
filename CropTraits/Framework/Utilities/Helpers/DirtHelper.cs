using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class DirtHelper
    {
        // Yields ground dirt and garden pot dirt, keyed by the tile they occupy
        public static IEnumerable<(Vector2 Tile, HoeDirt Dirt)> GetAllDirt(GameLocation location)
        {
            foreach (KeyValuePair<Vector2, TerrainFeature> pair in location.terrainFeatures.Pairs)
            {
                if (pair.Value is HoeDirt dirt)
                {
                    yield return (pair.Key, dirt);
                }
            }

            foreach (Object placedObject in location.objects.Values)
            {
                // Pot dirt is read from the pot's own tile since its HoeDirt isn't in terrainFeatures
                if (placedObject is IndoorPot pot && pot.hoeDirt.Value is not null)
                {
                    yield return (pot.TileLocation, pot.hoeDirt.Value);
                }
            }
        }

        // Captured before any mutation resolves so same-night mutations never influence each other
        public static Dictionary<Vector2, TileTraitState> CaptureTraitSnapshot(GameLocation location)
        {
            Dictionary<Vector2, TileTraitState> snapshot = new();

            foreach ((Vector2 tile, HoeDirt dirt) in GetAllDirt(location))
            {
                if (dirt.crop is null || dirt.crop.dead.Value)
                {
                    continue;
                }

                // Seed ID identifies the crop type since Data/Crops is keyed by it
                snapshot[tile] = new TileTraitState(dirt.crop.netSeedIndex.Value, TraitModDataHelper.GetTraits(dirt.modData));
            }

            return snapshot;
        }
    }
}
