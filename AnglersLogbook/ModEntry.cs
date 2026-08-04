using AnglersLogbook.Framework.Builders;
using AnglersLogbook.Framework.Managers;
using AnglersLogbook.Framework.Models.Fish;
using AnglersLogbook.Framework.Models.Stickers;
using AnglersLogbook.Framework.Utilities;
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

        public const string FISHING_TUTORIAL_EVENT_ID = "739330";

        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;

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
            if (e.Button is SButton.O && Context.IsPlayerFree && Game1.activeClickableMenu is null)
            {
                // Consume the button press
                Helper.Input.Suppress(e.Button);

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

            if (Game1.player.mailReceived.Contains("PeacefulEnd.Parchment.AnglersLogbook"))
            {
                return;
            }
            Game1.player.mailReceived.Add("PeacefulEnd.Parchment.AnglersLogbook");

            Item reward = ItemRegistry.Create("(O)74");
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
