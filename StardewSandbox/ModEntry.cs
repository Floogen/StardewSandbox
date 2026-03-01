using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using TimelessPocketWatch.Framework.Managers;
using TimelessPocketWatch.Framework.Objects;
using TimelessPocketWatch.Framework.Patches.Tools;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;

namespace TimelessPocketWatch
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;
        internal static ModConfig modConfig;

        internal static ApiManager apiManager;
        internal static AssetManager assetManager;
        internal static ToolManager toolManager;

        private float _pocketWatchEffectElapsedSeconds = 0;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
            modConfig = modHelper.ReadConfig<ModConfig>();

            // Create managers
            apiManager = new ApiManager(modHelper);
            assetManager = new AssetManager(modHelper);
            toolManager = new ToolManager(modHelper);

            try
            {
                var harmony = new Harmony(ModManifest.UniqueID);

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
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Display.RenderedWorld += OnDisplayRenderedWorld;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is null && e.Button is SButton.MouseRight && PocketWatch.IsToolPocketWatch(Game1.player.CurrentTool))
            {
                modConfig.AutoConsumeCharges = !modConfig.AutoConsumeCharges;

                var autoConsumeChargeMessage = modConfig.AutoConsumeCharges ? modHelper.Translation.Get("messages.info.auto_consume_enabled") : modHelper.Translation.Get("messages.info.auto_consume_disabled");
                Game1.addHUDMessage(new HUDMessage(autoConsumeChargeMessage, 2));
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
            {
                apiManager.RegisterGenericModConfigMenu(Helper, ModManifest);
            }
        }

        private void OnDisplayRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (toolManager.IsAnyPocketWatchActive())
            {
                if (modConfig.ShowGlowEffect)
                {
                    const float maxSize = 120f;
                    const float minSize = 40f;

                    float averageSize = (minSize + maxSize) / 2.0f;
                    float frequency = 12f;

                    float speed = 0.5f;
                    float oscillation = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * speed * Math.PI);
                    e.SpriteBatch.Draw(Game1.staminaRect, Vector2.Zero, Game1.lightmap.Bounds, new Color(50, 25, 25, (int)(averageSize + frequency * oscillation)), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                }

                if (modConfig.ShowClockEffect && _pocketWatchEffectElapsedSeconds <= 10)
                {
                    _pocketWatchEffectElapsedSeconds += (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                    var scale = Utility.Lerp(4f, 8f, _pocketWatchEffectElapsedSeconds * 2f);
                    if (scale < 8f)
                    {
                        var offset = new Vector2(32f, -16f);
                        var color = new Color(100, 100, 100, 100);

                        e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.player.Position) + offset, new Rectangle(224, 464, 41, 38), color, 0f, new Vector2(22.5f, 20f), scale, SpriteEffects.None, 0.98f);
                        e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.player.Position) + offset, Town.hourHandSource, color, (float)(Math.PI * 2.0 * (double)(Game1.timeOfDay % 1200 / 1200f) + (double)(Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes / 23f)), new Vector2(2.5f, 8f), scale, SpriteEffects.None, 0.98f);
                        e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.player.Position) + offset, Town.minuteHandSource, color, (float)(Math.PI * 2.0 * (double)(Game1.timeOfDay % 1000 % 100 % 60 / 60f) + (double)(Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 1.02f)), new Vector2(2.5f, 12f), scale, SpriteEffects.None, 0.99f);
                        e.SpriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.player.Position) + offset, Town.clockNub, color, 0f, new Vector2(2f, 2f), scale, SpriteEffects.None, 1f);
                    }
                }
            }
            else
            {
                _pocketWatchEffectElapsedSeconds = 0f;
            }
        }

        private void OnContentAssetRequested(object sender, AssetRequestedEventArgs e)
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
