using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewSandbox.Framework.Managers;
using StardewSandbox.Framework.Patches.Tools;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System;

namespace StardewSandbox
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;

        internal static AssetManager assetManager;
        internal static ToolManager toolManager;

        private float _pocketWatchEffectElapsedSeconds = 0;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Create managers
            assetManager = new AssetManager(modHelper);
            toolManager = new ToolManager(modHelper);

            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply patches
                new ToolPatch(monitor, modHelper).Apply(harmony);
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Hook into the required events
            helper.Events.Content.AssetRequested += OnContentAssetRequested;
            helper.Events.Display.RenderedWorld += Display_RenderedWorld;
        }

        private void Display_RenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (toolManager.IsAnyPocketWatchActive())
            {
                const float maxSize = 120f;
                const float minSize = 40f;

                float averageSize = (minSize + maxSize) / 2.0f;
                float frequency = 12f;

                float speed = 0.5f;
                float oscillation = (float)System.Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * speed * System.Math.PI);
                //monitor.Log((averageSize + (frequency * oscillation)).ToString(), LogLevel.Debug);
                e.SpriteBatch.Draw(Game1.staminaRect, Vector2.Zero, Game1.lightmap.Bounds, new Color(50, 25, 25, (int)(averageSize + (frequency * oscillation))), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                
                _pocketWatchEffectElapsedSeconds += (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                var scale = Utility.Lerp(4f, 8f, _pocketWatchEffectElapsedSeconds * 2f);
                if (scale < 8f)
                {
                    var offset = new Vector2(32f, -16f);
                    var color = new Color(100, 100, 100, 100);

                    e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.player.Position) + offset, new Rectangle(224, 464, 41, 38), color, 0f, new Vector2(22.5f, 20f), scale, SpriteEffects.None, 0.98f);
                    e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.player.Position) + offset, Town.hourHandSource, color, (float)(Math.PI * 2.0 * (double)((float)(Game1.timeOfDay % 1200) / 1200f) + (double)((float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes / 23f)), new Vector2(2.5f, 8f), scale, SpriteEffects.None, 0.98f);
                    e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.player.Position) + offset, Town.minuteHandSource, color, (float)(Math.PI * 2.0 * (double)((float)(Game1.timeOfDay % 1000 % 100 % 60) / 60f) + (double)((float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 1.02f)), new Vector2(2.5f, 12f), scale, SpriteEffects.None, 0.99f);
                    e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.player.Position) + offset, Town.clockNub, color, 0f, new Vector2(2f, 2f), scale, SpriteEffects.None, 1f);
                }

            }
            else
            {
                _pocketWatchEffectElapsedSeconds = 0f;
            }
        }

        private void OnContentAssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(assetManager.AddToolData, AssetEditPriority.Early);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(assetManager.AddArtifactData, AssetEditPriority.Early);
                e.Edit(assetManager.AddObjectData, AssetEditPriority.Early);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/MuseumRewards"))
            {
                e.Edit(assetManager.AddMuseumRewardsData, AssetEditPriority.Early);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(assetManager.AddCraftingRecipesData, AssetEditPriority.Early);
            }
            else if (assetManager.HasCustomAsset(e.NameWithoutLocale, out string assetPath))
            {
                e.LoadFromModFile<Texture2D>(assetPath, AssetLoadPriority.Exclusive);
            }
        }
    }
}
