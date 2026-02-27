using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewSandbox.Framework.Managers;
using StardewSandbox.Framework.Patches.Tools;
using StardewValley;
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
        }

        private void OnContentAssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(assetManager.AddToolData, AssetEditPriority.Early);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.POCKET_WATCH_TEXTURE_PATH))
            {
                e.LoadFromModFile<Texture2D>(assetManager.GetPocketWatchAssetPath(), AssetLoadPriority.Exclusive);
            }
        }
    }
}
