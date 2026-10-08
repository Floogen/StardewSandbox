using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.GiantCrops;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class GiantCropHelper
    {
        public const int GIANT_CROP_RANDOM_SEED = 6113; // Keeps the trait pick apart from other rolls made on the same tile and day
        public const int COLOSSAL_RANDOM_SEED = 6427; // Keeps the Colossal roll apart from the game's own giant crop roll

        // Set during the extra attempt Colossal makes, so that attempt doesn't roll its own bonus
        private static bool _isColossalAttempt;

        // Captured before the game removes the crops that form a giant crop, covering the largest giant crop this crop can become
        public static Dictionary<Vector2, TileTraitState> CaptureGrid(Crop crop, bool checkPreconditions)
        {
            GameLocation location = crop?.currentLocation;
            if (location is null || (checkPreconditions && crop.currentPhase.Value != crop.phaseDays.Count - 1))
            {
                return null;
            }

            if (crop.TryGetGiantCrops(out IReadOnlyList<KeyValuePair<string, GiantCropData>> giantCrops) is false)
            {
                return null;
            }

            int width = giantCrops.Max(entry => entry.Value.TileSize.X);
            int height = giantCrops.Max(entry => entry.Value.TileSize.Y);
            Vector2 origin = crop.tilePosition;

            Dictionary<Vector2, TileTraitState> grid = new();
            for (int y = (int)origin.Y; y < origin.Y + height; y++)
            {
                for (int x = (int)origin.X; x < origin.X + width; x++)
                {
                    Vector2 tile = new Vector2(x, y);
                    if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) && feature is HoeDirt dirt && dirt.crop is not null)
                    {
                        grid[tile] = new TileTraitState(dirt.crop.netSeedIndex.Value, TraitModDataHelper.GetTraits(dirt.modData));
                    }
                }
            }

            return grid;
        }

        // Called from the Crop.TryGrowGiantCrop postfix
        public static void HandleGrowthAttempt(Crop crop, bool checkPreconditions, Dictionary<Vector2, TileTraitState> grid, ref bool hasGrown)
        {
            if (grid is null || grid.Count == 0)
            {
                return;
            }

            // A successful Colossal attempt assigns its traits in its own postfix, so they're only assigned here for the game's own success
            if (hasGrown is false && checkPreconditions && _isColossalAttempt is false)
            {
                hasGrown = TryColossalAttempt(crop, grid);
            }
            else if (hasGrown)
            {
                AssignTraits(crop, grid);
            }
        }

        // Traits like Colossal add their own roll, then let the game form the giant crop with a roll that always passes so its grid and condition checks still decide
        public static bool TryColossalAttempt(Crop crop, Dictionary<Vector2, TileTraitState> grid)
        {
            if (crop.currentPhase.Value != crop.phaseDays.Count - 1)
            {
                return false;
            }

            double bonusChance = grid.Values.Max(state => CropTraits.traitManager.GetGiantCropBonusChance(state.Traits));
            if (bonusChance <= 0)
            {
                return false;
            }

            Random random = Utility.CreateDaySaveRandom(crop.tilePosition.X, crop.tilePosition.Y, COLOSSAL_RANDOM_SEED);
            if (random.NextDouble() >= bonusChance)
            {
                return false;
            }

            _isColossalAttempt = true;
            try
            {
                return crop.TryGrowGiantCrop(true, new GuaranteedRandom());
            }
            finally
            {
                _isColossalAttempt = false;
            }
        }

        // Picks up to three traits from the crops it formed from, so a trait shared by more of them is more likely to be picked
        public static void AssignTraits(Crop crop, Dictionary<Vector2, TileTraitState> grid)
        {
            GameLocation location = crop.currentLocation;
            Vector2 origin = crop.tilePosition;
            GiantCrop giantCrop = location?.resourceClumps.OfType<GiantCrop>().LastOrDefault(clump => clump.Tile == origin);
            if (giantCrop is null)
            {
                return;
            }

            Random random = Utility.CreateDaySaveRandom(origin.X, origin.Y, GIANT_CROP_RANDOM_SEED);

            // Sorted before shuffling so the pick only depends on the roll
            List<KeyValuePair<Vector2, TileTraitState>> formingCrops = grid
                .Where(entry => entry.Key.X < origin.X + giantCrop.width.Value && entry.Key.Y < origin.Y + giantCrop.height.Value)
                .OrderBy(entry => entry.Key.Y)
                .ThenBy(entry => entry.Key.X)
                .ToList();

            List<KeyValuePair<string, int>> candidates = formingCrops
                .SelectMany(entry => entry.Value.Traits.Entries)
                .ToList()
                .OrderBy(trait => random.Next())
                .ToList();

            TraitSet traits = new TraitSet();
            foreach (KeyValuePair<string, int> candidate in candidates)
            {
                if (traits.Has(candidate.Key) || CrossbreedHelper.CanPlaceTrait(candidate.Key, traits) is false)
                {
                    continue;
                }

                CrossbreedHelper.PlaceTrait(candidate.Key, candidate.Value, traits);
            }

            if (traits.Count == 0)
            {
                return;
            }

            TraitModDataHelper.SetTraits(giantCrop.modData, traits);

            string seedId = formingCrops.Select(entry => entry.Value.CropId).FirstOrDefault(cropId => string.IsNullOrEmpty(cropId) is false);
            if (seedId is not null)
            {
                giantCrop.modData[CropTraits.SEED_ID_MOD_DATA_KEY] = seedId;
            }
        }

        // Called from the GiantCrop.TryGetDrop postfix, so the produce keeps the giant crop's traits and traits like Titanic add to the stack
        public static void ApplyToDrop(GiantCrop giantCrop, Item drop, bool isShaving)
        {
            if (giantCrop is null || drop is null)
            {
                return;
            }

            TraitSet traits = TraitModDataHelper.GetTraits(giantCrop.modData);
            if (traits.Count == 0)
            {
                return;
            }

            // Only the produce itself, not any other drops the giant crop has
            string produceId = ItemRegistry.QualifyItemId(giantCrop.GetData()?.FromItemId);
            if (string.IsNullOrEmpty(produceId) || drop.QualifiedItemId != produceId)
            {
                return;
            }

            TraitModDataHelper.SetTraits(drop.modData, traits);
            if (giantCrop.modData.TryGetValue(CropTraits.SEED_ID_MOD_DATA_KEY, out string seedId))
            {
                drop.modData[CropTraits.SEED_ID_MOD_DATA_KEY] = seedId;
            }

            // Shaving drops come off while chopping, so only the final drop is boosted
            double yieldMultiplier = CropTraits.traitManager.GetGiantCropYieldMultiplier(traits);
            if (isShaving is false && yieldMultiplier != 1)
            {
                drop.Stack = Math.Max((int)Math.Ceiling(drop.Stack * yieldMultiplier), 1);
            }
        }
    }
}
