using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class WeedHelper
    {
        // Runs each morning (main player only), so weeds that spawned or spread overnight are already gone when the day starts
        public static void ClearAllNearbyWeeds()
        {
            if (Context.IsMainPlayer is false)
            {
                return;
            }

            Utility.ForEachLocation(location =>
            {
                try
                {
                    ClearNearbyWeeds(location);
                }
                catch (Exception exception)
                {
                    CropTraits.monitor.Log($"Failed to clear weeds around traited crops in {location.NameOrUniqueName}: {exception}", LogLevel.Warn);
                }

                return true;
            });
        }

        // Weed tiles are gathered first, since pot dirt is found by looking through the location's objects
        public static void ClearNearbyWeeds(GameLocation location)
        {
            HashSet<Vector2> weedTiles = new();
            foreach ((Vector2 tile, HoeDirt dirt) in DirtHelper.GetAllDirt(location))
            {
                if (dirt.crop is null || dirt.crop.dead.Value || CropTraits.traitManager.ClearsNearbyWeeds(TraitModDataHelper.GetTraits(dirt.modData)) is false)
                {
                    continue;
                }

                foreach (Vector2 neighborTile in Utility.getSurroundingTileLocationsArray(tile))
                {
                    if (location.objects.TryGetValue(neighborTile, out StardewValley.Object placedObject) && placedObject.IsWeeds())
                    {
                        weedTiles.Add(neighborTile);
                    }
                }
            }

            foreach (Vector2 weedTile in weedTiles)
            {
                location.objects.Remove(weedTile);
            }
        }
    }
}
