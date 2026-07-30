using AnglersLogbook.Framework.Models.Fish;
using AnglersLogbook.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.API;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnglersLogbook
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;

        public const string PARCHMENT_ID = "PeacefulEnd.Parchment.Core";
        public const string BOOK_DETAILS_ASSET = "Assets/PeacefulEnd.Parchment.AnglersLogbook/bookDetails";

        private IParchmentApi _parchmentApi;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

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
        }

        private void OnAssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(BOOK_DETAILS_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/bookDetails.png"), AssetLoadPriority.Low);
            }
        }

        private void OnButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.O && Context.IsPlayerFree && Game1.activeClickableMenu is null)
            {
                // Consume the button press
                Helper.Input.Suppress(e.Button);

                BuildBook();
                _parchmentApi.TryOpenBook("PeacefulEnd.Parchment.AnglersLogbook");
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
        }

        private void BuildBook()
        {
            var book = _parchmentApi.CreateBook("PeacefulEnd.Parchment.AnglersLogbook");

            // Setup the BookData
            book.Set("StartOnCover", true);
            book.Set("ExitToCover", true);

            book.AddOverlay("Image").Texture(BOOK_DETAILS_ASSET).TextureSource(0, 128, 32, 32).Scale(4).Position(-68, 125)
                .AddHoverFrame(0, 96)
                .Action("PeacefulEnd.Parchment_GoToStart")
                .Condition("ANY \"PeacefulEnd.Parchment_CurrentBookState Ready\" \"PeacefulEnd.Parchment_CurrentBookState Turning\"");

            book.Set("Layout.MarginTop", "20");
            book.Set("Layout.MarginSpine", "8");

            book.Set("Appearance.TintColor", "200 200 100 255");
            book.Set("Appearance.TexturePath", "Assets/PeacefulEnd.Parchment/notebook");
            book.Set("Appearance.GrayscaleTexturePath", "Assets/PeacefulEnd.Parchment/notebookGrayscale");
            book.Set("Appearance.OpenFrameCount", 6);

            book.Set("PageCurl.TexturePath", "Assets/PeacefulEnd.Parchment/curlPage2");
            book.Set("PageCurl.PreviousPageOffset", new Point(5, 105));
            book.Set("PageCurl.NextPageOffset", new Point(182, 105));

            // Build the cover
            book.AddOverlay("Banner").Texture(BOOK_DETAILS_ASSET).TextureSource(0, 64, 64, 32).Position(375, 125).Scale(3).Set("CapWidth", 29).Text("Fishing Log").Font("Dialogue").Condition("ANY \"PeacefulEnd.Parchment_CurrentBookState Cover\" \"PeacefulEnd.Parchment_CurrentBookState Sliding\"");
            //book.AddOverlay("Banner").Texture(BOOK_DETAILS_ASSET).TextureSource(0, 64, 96, 32).Position(325, 125).Scale(3).Set("CapWidth", 38).Text($"Fishing").Font("Dialogue").Condition("ANY \"PeacefulEnd.Parchment_CurrentBookState Cover\" \"PeacefulEnd.Parchment_CurrentBookState Sliding\"");
            book.AddOverlay("Image").Texture(BOOK_DETAILS_ASSET).TextureSource(0, 16, 14, 8).Position(350, 115).Scale(5).Origin(7, 4).Rotation(-45).Condition("ANY \"PeacefulEnd.Parchment_CurrentBookState Cover\" \"PeacefulEnd.Parchment_CurrentBookState Sliding\"");
            book.AddOverlay("Image").Texture(BOOK_DETAILS_ASSET).TextureSource(0, 16, 14, 8).Position(700, 200).Scale(5).Origin(7, 4).Rotation(-45).Condition("ANY \"PeacefulEnd.Parchment_CurrentBookState Cover\" \"PeacefulEnd.Parchment_CurrentBookState Sliding\"");

            book.AddOverlay("Image").Texture(BOOK_DETAILS_ASSET).TextureSource(96, 0, 48, 16).Position(400, 250).Origin(24, 8).Rotation(20).Scale(6).Condition("ANY \"PeacefulEnd.Parchment_CurrentBookState Cover\" \"PeacefulEnd.Parchment_CurrentBookState Sliding\"");

            var random = new Random();
            List<Point> stickerSpots = new List<Point>()
            {
                new Point(420, 350),
                new Point(320, 325),
                new Point(595, 315),
                new Point(625, 435),
                new Point(500, 450),
                new Point(320, 520),
                new Point(625, 540)
            };
            stickerSpots = stickerSpots.OrderBy(p => random.Next()).ToList();

            int stickerSpotIndex = 0;
            foreach (var fishSticker in GetRandomFishStickers(random, new Point(144, 0), 3, 8, new Point(32, 32), 8))
            {
                if (stickerSpotIndex >= stickerSpots.Count)
                {
                    break;
                }

                var stickerSpot = stickerSpots[stickerSpotIndex];
                book.AddOverlay("Image").Texture(BOOK_DETAILS_ASSET).TextureSource(fishSticker.X, fishSticker.Y, fishSticker.Width, fishSticker.Height).Position(stickerSpot.X, stickerSpot.Y).Origin(16, 16).Rotation(random.Next(-1, 1) * 45).Scale(random.Next(4, 6)).Set("SpriteEffects", random.NextBool(0.25f) ? "FlipHorizontally" : "None").Condition("ANY \"PeacefulEnd.Parchment_CurrentBookState Cover\" \"PeacefulEnd.Parchment_CurrentBookState Sliding\"");

                stickerSpotIndex++;
            }
            // Build the intro pages (first two seen on book open)
            var contentsPage = book.AddPage("contents", "front");

            var statsPage = book.AddPage("stats", "front");
            statsPage.AddTitle("Stats").Alignment("Center");

            GetUniqueFishStats(Game1.player, out int uniqueFishCaught, out int uniqueFishTotal);
            AddPanelWithTitle(statsPage, new Point(40, 100), "Unique\nFish Caught", $"{uniqueFishCaught} / {uniqueFishTotal}");
            AddPanelWithTitle(statsPage, new Point(-40, 100), "Total\nFish Caught", $"{Game1.player.stats.Get("PreciseFishCaught")}", alignment: "Right");
            AddPanelWithTitle(statsPage, new Point(40, 300), "Treasures Found", $"{Game1.player.stats.Get("FishingTreasures")}");
            AddPanelWithTitle(statsPage, new Point(-40, 300), "Fishing\nAttempts", $"{Game1.player.stats.Get("TimesFished")}", alignment: "Right");

            int gridColumns = 5;
            int gridRows = 5;
            int maxFishPerPage = gridColumns * gridRows;

            var collectableFishes = GetCollectableFishes();
            int fishPreviewPageCounter = 0;
            foreach (var pageFishes in collectableFishes.Chunk(maxFishPerPage).DefaultIfEmpty(Array.Empty<ParsedItemData>()))
            {
                var fishPreviewPage = book.AddPage($"fishPreview{fishPreviewPageCounter}", "front");
                if (fishPreviewPageCounter is 0)
                {
                    fishPreviewPage.AddTitle("Fish").Alignment("Center");
                }

                BuildFishPreviewGrid(book, fishPreviewPage, pageFishes, gridColumns);
                fishPreviewPageCounter += 1;
            }

            // Create fish specific pages
            List<FishEntry> allFish = FishDataHelper.GetAllFish();
            foreach (var fishData in allFish)
            {
                bool itemUnlocked = Game1.player.fishCaught.ContainsKey(fishData.QualifiedItemId);

                FishCatchInfo info = FishCatchHelper.GetCatchInfo(fishData.QualifiedItemId);
                int amountCaught = info.TimesCaught;
                string size = FishCatchHelper.FormatBiggestCatch(info);


                itemUnlocked = true;

                var fishPageId = $"fishChapter{fishData.ItemData.ItemId}";
                var fishPageSummary = book.AddPage(fishPageId, fishPageId);

                fishPageSummary.AddTitle(itemUnlocked ? fishData.ItemData.DisplayName : "???").Alignment("Center").Spacing(36);
                var fishPanel =  fishPageSummary.AddPanel().Spacing(32);
                fishPanel.AddBackground("Image").Texture(BOOK_DETAILS_ASSET).TextureSource(48, 0, 16, 16).Scale(8).Alignment("Center").Position(0, -24);
                fishPanel.AddChild("Image").Item(fishData.QualifiedItemId).Tint(itemUnlocked ? "255 255 255 255" : "0 0 0 89").Set("DisplayName", "").Set("Description", "")
                    .Scale(5).Alignment("Center")
                    .Spacing(8);
                fishPageSummary.AddParagraph(fishData.ItemData.Description).Alignment("Left").Margin(24, 0).Spacing(12);
                fishPageSummary.AddDivider().Tint("234 200 178").Width(384).Sizing("Fixed").Set("Thickness", 4).Alignment("Center").Spacing(16);
                fishPageSummary.AddHeading($"Stats").Alignment("Center").Margin(24, 0).Spacing(12);
                fishPageSummary.AddParagraph($"Amount caught: {amountCaught}\nLargest size: {(string.IsNullOrEmpty(size) ? "None" : size)}\nMax size: {fishData.FishData.MaxSize} inches").Alignment("Left").Margin(24, 0).Spacing(12);
                fishPageSummary.AddDivider().Tint("234 200 178").Width(384).Sizing("Fixed").Set("Thickness", 4).Alignment("Center").Spacing(16);


                var fishPageDetails = book.AddPage($"{fishPageId}_summary", fishPageId);
                fishPageDetails.AddTitle("Details").Alignment("Center").Spacing(36);
            }

            if (book.TryRegister(out var error) is false)
            {
                Monitor.Log($"Failed to register Parchment book: {error}", LogLevel.Warn);
            }
        }

        private List<Rectangle> GetRandomFishStickers(Random random, Point sheetOrigin, int columns, int rows, Point stickerSize, int count)
        {
            List<Rectangle> results = new List<Rectangle>();

            List<int> indices = Enumerable.Range(0, columns * rows).ToList();
            for (int i = 0; i < count && indices.Count > 0; i++)
            {
                int pick = random.Next(indices.Count);
                int index = indices[pick];
                indices.RemoveAt(pick);

                int x = sheetOrigin.X + (index % columns) * stickerSize.X;
                int y = sheetOrigin.Y + (index / columns) * stickerSize.Y;
                results.Add(new Rectangle(x, y, stickerSize.X, stickerSize.Y));
            }

            return results;
        }


        private void BuildFishPreviewGrid(IBookBuilder book, IPageBuilder page, IReadOnlyList<ParsedItemData> fishes, int maxColumns)
        {
            int startingColumnX = 40;
            int startingRowY = 100;
            int cellSize = 75;

            for (int index = 0; index < fishes.Count; index++)
            {
                var fishData = fishes[index];
                var point = new Point(startingColumnX + index % maxColumns * cellSize, startingRowY + index / maxColumns * cellSize);

                AddCollectableItemWithBackground(book, page, point, fishData, Game1.player.fishCaught.ContainsKey(fishData.QualifiedItemId), "Left");
            }
        }

        private void AddCollectableItemWithBackground(IBookBuilder book, IPageBuilder page, Point point, ParsedItemData itemData, bool itemUnlocked, string alignment = "Left")
        {
            var fishPageId = $"fishChapter{itemData.ItemId}";

            page.AddBackground("Image").Texture(BOOK_DETAILS_ASSET).TextureSource(48, 0, 16, 16).Scale(4).Alignment(alignment).Position(point.X, point.Y);
            page.AddBackground("Image").Item(itemData.QualifiedItemId).Tint(itemUnlocked ? "255 255 255 255" : "0 0 0 89").Set("DisplayName", itemUnlocked ? itemData.DisplayName : "???")
                .Set("Description", "").Scale(3).Alignment(alignment).Position(point.X + 8, point.Y + 8).Origin(8, 8)
                .AddHoverFrameInPlace(100, 1f).AddHoverFrameInPlace(100, 1.1f).AddHoverFrameInPlace(100, 1.15f).AddHoverFrameInPlace(100, 1.1f)
                .Action($"PeacefulEnd.Parchment_JumpToChapter {fishPageId}");
        }

        private void AddPanelWithTitle(IPageBuilder page, Point point, string panelTitle, string panelText, int parentPanelWidth = 150, int childPanelWidth = 25, string alignment = "Left")
        {
            var parentPanel = page.AddBackground("Panel").Width(parentPanelWidth).Sizing("Fixed").Padding(0).Alignment(alignment).Position(point.X, point.Y);
            parentPanel.AddChild("Paragraph").Text(panelTitle).Alignment("Center");

            var childPanel = parentPanel.AddChild("Panel").Texture(BOOK_DETAILS_ASSET).TextureSource(64, 32, 24, 24).Scale(3).Width(childPanelWidth).Sizing("Fixed").Padding(0).Alignment("Center");
            childPanel.AddChild("Paragraph").Text(panelText).Alignment("Center");
        }

        private void GetUniqueFishStats(Farmer who, out int uniqueFishCaught, out int uniqueFishTotal)
        {
            uniqueFishCaught = 0;
            uniqueFishTotal = 0;

            foreach (var fishData in GetCollectableFishes())
            {
                uniqueFishTotal += 1;
                if (Game1.player.fishCaught.ContainsKey(fishData.QualifiedItemId))
                {
                    uniqueFishCaught += 1;
                }
            }
        }

        private List<ParsedItemData> GetCollectableFishes()
        {
            List<ParsedItemData> collectableFishes = new List<ParsedItemData>();
            foreach (var fishData in ItemRegistry.GetObjectTypeDefinition().GetAllData().Where(d => d.ObjectType.EqualsIgnoreCase("Fish")))
            {
                ObjectData objectData = fishData.RawData as ObjectData;
                if (objectData is not null && objectData.ExcludeFromFishingCollection)
                {
                    continue;
                }

                collectableFishes.Add(fishData);
            }

            return collectableFishes;
        }
    }
}
