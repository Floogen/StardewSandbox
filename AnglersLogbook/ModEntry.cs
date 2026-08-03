using AnglersLogbook.Framework.Builders;
using AnglersLogbook.Framework.Managers;
using AnglersLogbook.Framework.Models.Fish;
using AnglersLogbook.Framework.Models.Stickers;
using AnglersLogbook.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.API;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnglersLogbook
{
    public class ModEntry : Mod
    {
        // Constants
        public const string PARCHMENT_ID = "PeacefulEnd.Parchment.Core";
        public const string BOOK_ID = "PeacefulEnd.Parchment.AnglersLogbook";

        public const string BOOK_DETAILS_ASSET = "Assets/PeacefulEnd.Parchment.AnglersLogbook/bookDetails";
        public const string NOTEBOOK_ASSET = "Assets/PeacefulEnd.Parchment.AnglersLogbook/notebook";
        public const string NOTEBOOK_GRAYSCALE_ASSET = "Assets/PeacefulEnd.Parchment.AnglersLogbook/notebookGrayscale";

        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;

        // Builders
        internal static LogbookBuilder logbookBuilder;

        // Managers
        internal static StickerManager stickerManager;

        private IParchmentApi _parchmentApi;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Create managers
            stickerManager = new StickerManager(monitor, helper);

            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply patches
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Hook into the required events
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Content.AssetRequested += OnAssetRequested;

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        }

        private void OnAssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(BOOK_DETAILS_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/bookDetails.png"), AssetLoadPriority.Low);
            }
            if (e.NameWithoutLocale.IsEquivalentTo(NOTEBOOK_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/notebook.png"), AssetLoadPriority.Low);
            }
            if (e.NameWithoutLocale.IsEquivalentTo(NOTEBOOK_GRAYSCALE_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/notebookGrayscale.png"), AssetLoadPriority.Low);
            }
        }

        private void OnButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.O && Context.IsPlayerFree && Game1.activeClickableMenu is null)
            {
                // Consume the button press
                Helper.Input.Suppress(e.Button);

                logbookBuilder.OpenBook();
            }
        }

        private void OnGameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            if (Helper.ModRegistry.IsLoaded(PARCHMENT_ID) is false)
            {
                return;
            }

            _parchmentApi = Helper.ModRegistry.GetApi<IParchmentApi>(PARCHMENT_ID);
            if (_parchmentApi is null)
            {
                Monitor.Log("Parchment is installed but its API could not be loaded!", LogLevel.Warn);
                return;
            }

            // Create the book builder
            logbookBuilder = new LogbookBuilder(_parchmentApi);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Generate stickers
            logbookBuilder.GenerateStickers();

            // Track new fish being caught
            Game1.player.fishCaught.OnValueAdded += OnFishCaught;
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            if (Game1.player is not null)
            {
                Game1.player.fishCaught.OnValueAdded -= OnFishCaught;
            }
        }

        private void OnFishCaught(string key, int[] value)
        {
            // Generate stickers
            logbookBuilder.GenerateStickers();
        }
    }
}
