using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace CropTraits.Framework.Managers
{
    public class TraitViewManager : BaseManager
    {
        // Per screen, so each split-screen player toggles their own view
        private readonly PerScreen<bool> _isEnabled = new();

        public bool IsEnabled => _isEnabled.Value;

        public TraitViewManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (Context.IsPlayerFree is false || CropTraits.modConfig.ToggleTraitViewKeybind.JustPressed() is false)
            {
                return;
            }

            helper.Input.SuppressActiveKeybinds(CropTraits.modConfig.ToggleTraitViewKeybind);

            _isEnabled.Value = _isEnabled.Value is false;
            Game1.playSound("smallSelect");
        }

        public void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (IsEnabled is false || Context.IsWorldReady is false || Game1.eventUp || Game1.currentLocation is null)
            {
                return;
            }

            foreach ((Vector2 tile, HoeDirt dirt) in GetVisibleDirt(Game1.currentLocation))
            {
                if (PlantedTraitHelper.GetVisibleTraits(dirt).Count > 0)
                {
                    MutationBadgeHelper.DrawCropBadge(e.SpriteBatch, tile);
                }
            }
        }

        public void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (IsEnabled is false || Context.IsPlayerFree is false || Game1.currentLocation is null)
            {
                return;
            }

            Vector2 hoveredTile = Game1.lastCursorMotionWasMouse ? Game1.currentCursorTile : Game1.player.GetGrabTile();
            if (TryGetHoveredCrop(Game1.currentLocation, hoveredTile, out HoeDirt dirt, out Vector2 cropTile))
            {
                CropTooltipHelper.Draw(e.SpriteBatch, dirt, cropTile);
            }
        }

        public void Reset()
        {
            _isEnabled.ResetAllScreens();
        }

        private bool TryGetHoveredCrop(GameLocation location, Vector2 hoveredTile, out HoeDirt dirt, out Vector2 cropTile)
        {
            cropTile = hoveredTile;
            if (PlantedTraitHelper.TryGetDirt(location, hoveredTile, out dirt) && dirt.crop is not null)
            {
                return PlantedTraitHelper.GetVisibleTraits(dirt).Count > 0;
            }

            cropTile = hoveredTile + new Vector2(0, 1);
            return PlantedTraitHelper.TryGetDirt(location, cropTile, out dirt) && PlantedTraitHelper.GetVisibleTraits(dirt).Count > 0;
        }

        // Only the tiles on screen are checked, plus one extra row below since badges sit at the top of their tile
        private IEnumerable<(Vector2 Tile, HoeDirt Dirt)> GetVisibleDirt(GameLocation location)
        {
            int minX = (Game1.viewport.X / Game1.tileSize) - 1;
            int minY = (Game1.viewport.Y / Game1.tileSize) - 1;
            int maxX = ((Game1.viewport.X + Game1.viewport.Width) / Game1.tileSize) + 1;
            int maxY = ((Game1.viewport.Y + Game1.viewport.Height) / Game1.tileSize) + 2;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 tile = new Vector2(x, y);
                    if (PlantedTraitHelper.TryGetDirt(location, tile, out HoeDirt dirt))
                    {
                        yield return (tile, dirt);
                    }
                }
            }
        }
    }
}
