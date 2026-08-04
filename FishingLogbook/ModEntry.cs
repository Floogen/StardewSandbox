using FishingLogbook.Framework;
using FishingLogbook.Framework.Builders;
using FishingLogbook.Framework.External.API.GMCM;
using FishingLogbook.Framework.Managers;
using FishingLogbook.Framework.Models.Fish;
using FishingLogbook.Framework.Models.Stickers;
using FishingLogbook.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.API;
using Parchment.Framework.Patches.Objects;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FishingLogbook
{
    public class ModEntry : Mod
    {
        // Constants
        public const string PARCHMENT_ID = "PeacefulEnd.Parchment.Core";
        public const string BOOK_ID = "PeacefulEnd.Parchment.FishingLogbook";

        public const string BOOK_DETAILS_ASSET = "Assets/PeacefulEnd.Parchment.FishingLogbook/bookDetails";
        public const string NOTEBOOK_ASSET = "Assets/PeacefulEnd.Parchment.FishingLogbook/notebook";
        public const string NOTEBOOK_GRAYSCALE_ASSET = "Assets/PeacefulEnd.Parchment.FishingLogbook/notebookGrayscale";

        public const string FISHING_TUTORIAL_EVENT_ID = "739330";

        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;
        internal static Config config;

        // Builders
        internal static LogbookBuilder logbookBuilder;

        // Managers
        internal static StickerManager stickerManager;

        private IParchmentApi _parchmentApi;
        private string _activeEventId;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
            config = helper.ReadConfig<Config>();

            // Create managers
            stickerManager = new StickerManager(monitor, helper);

            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply patches
                new FarmerPatch(monitor, modHelper).Apply(harmony);
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
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;

                    data[BOOK_ID] = new ObjectData()
                    {
                        Name = BOOK_ID,
                        DisplayName = "Fishing Log",
                        Description = "A handy notebook for logging your catches.",
                        Type = "Basic",
                        Category = 0,
                        Price = 0,
                        Texture = BOOK_DETAILS_ASSET,
                        SpriteIndex = 0,
                        Edibility = -300,
                        CustomFields = new Dictionary<string, string>()
                        {
                            { "PeacefulEnd.Parchment/CustomFields/BookId", BOOK_ID }
                        }
                    };
                }, AssetEditPriority.Early);
            }
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ShopData>().Data;

                    if (data.TryGetValue("FishShop", out ShopData shop) is false)
                    {
                        return;
                    }

                    shop.Items.Add(new ShopItemData()
                    {
                        Id = BOOK_ID,
                        ItemId = $"(O){BOOK_ID}",
                        Price = 0,
                        AvailableStock = 1,
                        AvailableStockLimit = LimitedStockMode.Player
                    });
                });
            }
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

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (config.LogbookShortcut is not null && config.LogbookShortcut.IsDown() && _parchmentApi is not null && Context.IsPlayerFree && Game1.activeClickableMenu is null)
            {
                logbookBuilder.OpenBook();
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
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

            var gmcmApi = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcmApi is not null)
            {
                gmcmApi.Register(
                    mod: ModManifest,
                    reset: () => config = new Config(),
                    save: () => modHelper.WriteConfig(config)
                );

                gmcmApi.AddKeybindList(
                    mod: ModManifest,
                    name: () => "Open Fishing Log Shortcut",
                    getValue: () => config.LogbookShortcut,
                    setValue: value => config.LogbookShortcut = value
                );
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

            // Register the book
            if (logbookBuilder.Build().TryRegister(out var error))
            {

            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Context.IsWorldReady is false)
            {
                return;
            }

            if (Game1.CurrentEvent is Event currentEvent)
            {
                _activeEventId = currentEvent.id;
                return;
            }

            if (string.IsNullOrEmpty(_activeEventId))
            {
                return;
            }

            // Cache and clear first, so an early return can't leave us re-triggering
            string finishedEventId = _activeEventId;
            _activeEventId = null;

            if (finishedEventId != FISHING_TUTORIAL_EVENT_ID)
            {
                return;
            }

            if (Game1.player.mailReceived.Contains("PeacefulEnd.Parchment.FishingLogbook"))
            {
                return;
            }
            Game1.player.mailReceived.Add("PeacefulEnd.Parchment.FishingLogbook");

            Item reward = ItemRegistry.Create($"(O){BOOK_ID}");
            Game1.player.holdUpItemThenMessage(reward);
            Game1.player.addItemByMenuIfNecessary(reward);
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
