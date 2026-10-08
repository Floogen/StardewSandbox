using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Managers
{
    public class CropLightManager : BaseManager
    {
        public const string LIGHT_ID_PREFIX = "PeacefulEnd.CropTraits_Glow_";
        public const int REFRESH_TICKS = 30;
        public const float LIGHT_RADIUS = 1f;

        // Light colors are subtracted from the darkness, so this gives a soft green glow
        public static readonly Color LIGHT_COLOR = new Color(120, 40, 120);

        private readonly HashSet<string> _lightIds = new();

        /// <summary>Adds a local light to each glowing crop in the current location while it's dark out, removing them otherwise.</summary>
        public CropLightManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Context.IsWorldReady is false || e.IsMultipleOf(REFRESH_TICKS) is false)
            {
                return;
            }

            RefreshLights();
        }

        public void RefreshLights()
        {
            GameLocation location = Game1.currentLocation;
            HashSet<string> wantedIds = new();

            if (location is not null && Game1.isDarkOut(location))
            {
                foreach ((Vector2 tile, HoeDirt dirt) in DirtHelper.GetAllDirt(location))
                {
                    if (dirt.crop is null || dirt.crop.dead.Value || CropTraits.traitManager.GlowsAtNight(CropAppearanceHelper.GetVisibleTraits(dirt)) is false)
                    {
                        continue;
                    }

                    string lightId = $"{LIGHT_ID_PREFIX}{location.NameOrUniqueName}_{tile.X}_{tile.Y}";
                    wantedIds.Add(lightId);

                    if (Game1.currentLightSources.ContainsKey(lightId) is false)
                    {
                        Vector2 position = (tile * Game1.tileSize) + new Vector2(Game1.tileSize / 2f);
                        Game1.currentLightSources[lightId] = new LightSource(lightId, LightSource.lantern, position, LIGHT_RADIUS, LIGHT_COLOR, LightSource.LightContext.None, 0, location.NameOrUniqueName);
                    }
                }
            }

            foreach (string lightId in _lightIds.Where(lightId => wantedIds.Contains(lightId) is false).ToList())
            {
                Game1.currentLightSources.Remove(lightId);
            }

            _lightIds.Clear();
            _lightIds.UnionWith(wantedIds);
        }

        public void Reset()
        {
            foreach (string lightId in _lightIds)
            {
                Game1.currentLightSources.Remove(lightId);
            }

            _lightIds.Clear();
        }
    }
}
