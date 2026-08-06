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
using System.Transactions;

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
        internal static Config config;
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;
        internal static ITranslationHelper translation;

        // Builders
        internal static LogbookBuilder logbookBuilder;

        // Managers
        internal static StickerManager stickerManager;

        private IParchmentApi _parchmentApi;
        private string _activeEventId;
        private bool _skipDayStartedRefresh;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            config = helper.ReadConfig<Config>();
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
            translation = helper.Translation;

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
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
            helper.Events.Display.MenuChanged += OnMenuChanged;

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
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
                        DisplayName = translation.Get("item.fishingLog.name"),
                        Description = translation.Get("item.fishingLog.description"),
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

        private void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
        {
            foreach (var assetName in e.NamesWithoutLocale)
            {
                if (assetName.IsEquivalentTo("Data/Fish") is false && assetName.IsEquivalentTo("Data/Locations") is false && assetName.IsEquivalentTo("Data/Objects") is false && assetName.IsEquivalentTo("Data/WorldMap") is false)
                {
                    continue;
                }

                FishDataHelper.ClearCache();
                DetailPagePlanner.ClearPlans();
                logbookBuilder?.QueueRefresh();
                return;
            }
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is null)
            {
                return;
            }

            // Catches the book being opened through its item, which Parchment handles without going through OpenBook
            logbookBuilder?.OnMenuOpened();
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
                    name: () => translation.Get("config.logbookShortcut.name").ToString(),
                    getValue: () => config.LogbookShortcut,
                    setValue: value => config.LogbookShortcut = value
                );
            }

            // Create the book builder
            logbookBuilder = new LogbookBuilder(_parchmentApi);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // The stickers are laid out by the build below, rather than twice over
            logbookBuilder.QueueStickerRefresh();

            // Track new fish being caught
            Game1.player.fishCaught.OnValueAdded += OnFishCaught;

            // Registered up front, since the book's item has to work before anything marks it stale
            logbookBuilder.RefreshBook();

            // DayStarted follows this on the day the save is loaded, so the book is already current for it
            _skipDayStartedRefresh = true;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (_skipDayStartedRefresh)
            {
                _skipDayStartedRefresh = false;
                return;
            }

            // Rebuild so season and weather driven availability isn't left over from yesterday
            logbookBuilder?.QueueRefresh();
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Context.IsWorldReady is false)
            {
                return;
            }

            if (logbookBuilder is not null)
            {
                logbookBuilder.UpdateQueuedRefresh();
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

            // Required to clear the cached farm type location
            FishDataHelper.ClearCache();
            DetailPagePlanner.ClearPlans();

            if (logbookBuilder is not null)
            {
                logbookBuilder.Reset();
            }

            _skipDayStartedRefresh = false;
        }

        private void OnFishCaught(string key, int[] value)
        {
            // Laying the stickers out here would land on the frame the fish is caught, so it waits for the next build
            logbookBuilder.QueueStickerRefresh();
        }
    }
}
