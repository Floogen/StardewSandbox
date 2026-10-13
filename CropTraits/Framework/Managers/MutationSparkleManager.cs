using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Managers
{
    /// <summary>
    /// Makes mature mutated crops twinkle until their first harvest, with each sparkle centered on a visible pixel of the crop's sprite.
    /// Each crop rolls on its own, so a field twinkles at random rather than all at once. The sprites are only added locally.
    /// </summary>
    public class MutationSparkleManager : BaseManager
    {
        public const uint CHECK_INTERVAL_TICKS = 120;
        public const double SPARKLE_CHANCE_PER_CHECK = 0.25;
        public const int SPARKLE_DURATION_MILLISECONDS = 1400;
        public const int MILLISECONDS_BETWEEN_SPARKLES = 500;
        public const string CROP_DATA_PATH = "Data/Crops";
        public const string DEFAULT_CROP_TEXTURE_PATH = "TileSheets/crops";

        private readonly Random _random = new Random();

        // Reading pixels is slow, so each frame is only measured once. An empty list means the frame is fully transparent
        private readonly Dictionary<(Texture2D Texture, Rectangle SourceRect), List<Point>> _sparklePointsCache = new();

        public MutationSparkleManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        /// <summary>
        /// Clears the measurements only when a crop texture (or the crop data naming them) was invalidated, since other mods invalidate assets often.
        /// A retexture can change a crop's shape without replacing its texture instance, so every measurement is redone in that case.
        /// </summary>
        public void HandleAssetsInvalidated(AssetsInvalidatedEventArgs e)
        {
            if (_sparklePointsCache.Count == 0)
            {
                return;
            }

            HashSet<string> cropTexturePaths = GetCropTexturePaths();
            if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo(CROP_DATA_PATH) || cropTexturePaths.Any(texturePath => name.IsEquivalentTo(texturePath))))
            {
                _sparklePointsCache.Clear();
            }
        }

        // Crops without their own texture use the vanilla crop sheet
        private HashSet<string> GetCropTexturePaths()
        {
            HashSet<string> texturePaths = new() { DEFAULT_CROP_TEXTURE_PATH };
            foreach (CropData cropData in Game1.cropData.Values)
            {
                if (string.IsNullOrEmpty(cropData?.Texture) is false)
                {
                    texturePaths.Add(cropData.Texture);
                }
            }

            return texturePaths;
        }

        public void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (CropTraits.modConfig.ShowMutationSparkles is false || Context.IsPlayerFree is false || Game1.game1.IsActive is false || e.IsMultipleOf(CHECK_INTERVAL_TICKS) is false)
            {
                return;
            }

            GameLocation location = Game1.currentLocation;
            if (location is null)
            {
                return;
            }

            foreach ((Vector2 tile, HoeDirt dirt) in DirtHelper.GetAllDirt(location))
            {
                if (_random.NextDouble() >= SPARKLE_CHANCE_PER_CHECK || Utility.isOnScreen(tile * Game1.tileSize, Game1.tileSize * 2) is false || PlantedTraitHelper.IsUnharvestedMutation(dirt) is false)
                {
                    continue;
                }

                Crop crop = dirt.crop;
                List<Point> framePoints = GetFramePoints(crop);
                if (framePoints.Count == 0)
                {
                    continue;
                }

                bool isInPot = location.objects.TryGetValue(tile, out StardewValley.Object placedObject) && placedObject is IndoorPot;
                CropSparkleHelper.AddSparkles(location, () => CropSparkleHelper.GetWorldPosition(framePoints[_random.Next(framePoints.Count)], crop.sourceRect.Width, tile, crop.flip.Value, isInPot, _random), CropSparkleHelper.GetLayerDepth(tile), SPARKLE_DURATION_MILLISECONDS, MILLISECONDS_BETWEEN_SPARKLES, Color.White, _random);
            }
        }

        private List<Point> GetFramePoints(Crop crop)
        {
            Texture2D texture = crop.DrawnCropTexture;
            List<Point> framePoints = GetSparklePoints(texture, crop.sourceRect);
            if (crop.programColored.Value is false)
            {
                return framePoints;
            }

            // Copied so the cached lists are never changed
            List<Point> combinedPoints = new List<Point>(framePoints);
            combinedPoints.AddRange(GetSparklePoints(texture, crop.coloredSourceRect));
            return combinedPoints;
        }

        private List<Point> GetSparklePoints(Texture2D texture, Rectangle sourceRect)
        {
            if (texture is null || sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                return new List<Point>();
            }

            if (_sparklePointsCache.TryGetValue((texture, sourceRect), out List<Point> sparklePoints))
            {
                return sparklePoints;
            }

            try
            {
                sparklePoints = CropSparkleHelper.GetSparklePoints(texture, sourceRect);
            }
            catch (Exception exception)
            {
                // Falls back to the whole frame, so the crop still sparkles
                monitor.LogOnce($"Couldn't read crop pixels for sparkles, using the full frame instead: {exception.Message}", LogLevel.Trace);
                sparklePoints = CropSparkleHelper.GetAllPoints(sourceRect);
            }

            _sparklePointsCache[(texture, sourceRect)] = sparklePoints;
            return sparklePoints;
        }
    }
}
