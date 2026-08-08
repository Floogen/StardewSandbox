using FishingLogbook.Framework.Managers;
using FishingLogbook.Framework.Models.Fish;
using FishingLogbook.Framework.Models.Stickers;
using FishingLogbook.Framework.Utilities;
using Microsoft.Xna.Framework;
using Parchment.Framework.API;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FishingLogbook.Framework.Builders
{
    /// <summary>
    /// Handles creating the Parchment logbook.
    /// </summary>
    public class LogbookBuilder
    {
        // Constants
        public const string TOGGLE_BOOKMARK = "PeacefulEnd.Parchment.FishingLogbook_ToggleBookmark";

        private const string COVER_STATE_CONDITION = "ANY \"PeacefulEnd.Parchment_CurrentBookState Cover\" \"PeacefulEnd.Parchment_CurrentBookState Sliding\"";
        private const string READABLE_STATE_CONDITION = "ANY \"PeacefulEnd.Parchment_CurrentBookState Ready\" \"PeacefulEnd.Parchment_CurrentBookState Turning\"";

        private const int REFRESH_DELAY_TICKS = 10;
        private const int SWAP_ATTEMPT_LIMIT = 60;

        private IParchmentApi _parchmentApi;
        private List<StickerPlacement> _stickerPlacements;

        private bool _areStickersStale;
        private int _ticksUntilRefresh;

        private IBookBuilder _pendingSwap;
        private int _swapAttemptsRemaining;

        public LogbookBuilder(IParchmentApi api)
        {
            _parchmentApi = api;

            TriggerActionManager.RegisterAction(TOGGLE_BOOKMARK, ToggleBookmark);
        }

        /// <summary>
        /// Creates the stickers by generating a new Texture2D with a white outline. Should only be used when new fish are caught or on the first open of the save.
        /// </summary>
        public void GenerateStickers()
        {
            _areStickersStale = false;

            var stickers = ModEntry.stickerManager.GetRandomStickers(Game1.player, count: 64, skipCaughtRequirement: false);
            var exclusions = new[] { new Rectangle(435, 250, 220, 80) };
            _stickerPlacements = StickerLayoutHelper.Arrange(stickers, new Rectangle(325, 260, 440, 375), exclusions, minScale: 4, maxScale: 5, largeScaleChance: 0.15f, gridStep: 4, spacing: 4, packingSamples: 20, preserveOrder: false);

            ModEntry.stickerManager.Prepare(_stickerPlacements.Select(p => p.Sticker));
        }

        /// <summary>
        /// Create the various pages and returns the Parchment book for opening.
        /// </summary>
        public IBookBuilder Build()
        {
            // The stickers are only seen on the cover, so their layout waits until the book is actually being put together
            if (_areStickersStale || _stickerPlacements is null)
            {
                GenerateStickers();
            }

            var collectableFishes = FishDataHelper.GetAllFish();

            var book = _parchmentApi.CreateBook(ModEntry.BOOK_ID);
            book.AddVariable("forceFishNames").Type("Boolean").Default("false").Scope("Global");
            book.AddVariable("startOnCover").Type("Boolean").Default("true").Scope("Global");
            book.AddVariable("exitToCover").Type("Boolean").Default("true").Scope("Global");
            book.AddVariable("bookmark1").Type("Text").Default("none").Scope("Save");
            book.AddVariable("bookmark2").Type("Text").Default("none").Scope("Save");
            book.AddVariable("bookmark3").Type("Text").Default("none").Scope("Save");
            book.AddVariable("bookmark4").Type("Text").Default("none").Scope("Save");
            book.AddVariable("bookmark5").Type("Text").Default("none").Scope("Save");

            book.OnRefresh(RefreshBook);

            // Setup the BookData
            if (_parchmentApi.TryGetVariable(ModEntry.BOOK_ID, "startOnCover", out var startOnCoverValue))
            {
                book.Set("StartOnCover", startOnCoverValue);
            }
            if (_parchmentApi.TryGetVariable(ModEntry.BOOK_ID, "exitToCover", out var exitToCoverValue))
            {
                book.Set("ExitToCover", exitToCoverValue);
            }

            book.AddOverlay("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(0, 192, 32, 32).Scale(4).Position(-68, 125)
                .Set("Description", ModEntry.translation.Get("ui.tooltip.goToStats").ToString())
                .AddHoverFrame(0, 160)
                .Action("PeacefulEnd.Parchment_GoToStart")
                .Condition(READABLE_STATE_CONDITION);
            book.AddOverlay("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(0, 128, 32, 32).Scale(4).Position(-68, 245)
                .Set("Description", ModEntry.translation.Get("ui.tooltip.goToFishOverview").ToString())
                .AddHoverFrame(0, 96)
                .Action("PeacefulEnd.Parchment_JumpToPageId fishPreview0")
                .Condition(READABLE_STATE_CONDITION);

            book.AddUnderlay("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(32, 168, 96, 16).Scale(5).Position(565, 0)
                .AddFrame(32, 170, 100, condition: $"PeacefulEnd.Parchment_CurrentPageHasTag Searchable")
                .AddFrame(32, 172, 100, condition: $"PeacefulEnd.Parchment_CurrentPageHasTag Searchable")
                .AddFrame(32, 174, 100, condition: $"PeacefulEnd.Parchment_CurrentPageHasTag Searchable")
                .AddFrame(32, 176, int.MaxValue, condition: $"PeacefulEnd.Parchment_CurrentPageHasTag Searchable").FrameAction("PeacefulEnd.Parchment_SetFlag searchBarReady")
                .AddFrame(32, 176, 100, condition: $"!PeacefulEnd.Parchment_CurrentPageHasTag Searchable").FrameAction("PeacefulEnd.Parchment_ClearFlag searchBarReady")
                .AddFrame(32, 174, 100, condition: $"!PeacefulEnd.Parchment_CurrentPageHasTag Searchable")
                .AddFrame(32, 172, 100, condition: $"!PeacefulEnd.Parchment_CurrentPageHasTag Searchable")
                .AddFrame(32, 170, int.MaxValue, condition: $"!PeacefulEnd.Parchment_CurrentPageHasTag Searchable")
                .Condition(READABLE_STATE_CONDITION);

            book.AddOverlay("Input").InputId("search").Placeholder(ModEntry.translation.Get("ui.search.placeholder")).Set("Height", 12).Width(96).Sizing("Fixed").Font("Small").Scale(2)
                .Position(735, 45)
                .Action("PeacefulEnd.Parchment_GoToStart")
                .TextChangedDelay(10f)
                .TextChangedAction("If PeacefulEnd.Parchment_HasInputText search ## PeacefulEnd.Parchment_JumpToPageId searchPage searchChapter true")
                .Condition("PeacefulEnd.Parchment_HasFlag searchBarReady, PeacefulEnd.Parchment_CurrentPageHasTag Searchable, PeacefulEnd.Parchment_CurrentBookState Ready");

            var bookmarkGrid = book.AddOverlay("Grid").Position(1035, 200)
                .Set("CellWidth", 128).Set("CellHeight", 64).Set("Columns", 1).Set("Rows", 5)
                .Set("RowSpacing", 8)
                .Condition(READABLE_STATE_CONDITION);

            for (int slot = 1; slot <= 5; slot++)
            {
                if (_parchmentApi.TryGetVariable(ModEntry.BOOK_ID, $"bookmark{slot}", out string itemId) is false || itemId.EqualsIgnoreCase("none"))
                {
                    continue;
                }

                var fishData = collectableFishes.FirstOrDefault(fish => fish.QualifiedItemId.EqualsIgnoreCase(itemId));
                if (fishData is null)
                {
                    continue;
                }
                var bookmarkPanel = bookmarkGrid.AddChild("Panel")
                    .Condition($"!PeacefulEnd.Parchment_HasVariable {ModEntry.BOOK_ID} bookmark{slot} none, ANY \"PeacefulEnd.Parchment_CurrentBookState Ready\" \"PeacefulEnd.Parchment_CurrentBookState Turning\"");

                bookmarkPanel.AddChild("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(64, 64, 32, 17).Scale(4).Alignment("Center")
                    .Set("Description", ModEntry.translation.Get("ui.tooltip.goToBookmark", new { fishName = GetFishDisplayName(Game1.player, fishData) }).ToString())
                    .Tint(nameof(Color.MediumSeaGreen))
                    .AddFrameInPlace()
                    .FrameAction($"PeacefulEnd.Parchment_ClearFlag bookmark{fishData.ItemData.ItemId}")
                    .AddHoverFrame(64, 81)
                    .HoverAction($"PeacefulEnd.Parchment_SetFlag bookmark{fishData.ItemData.ItemId}")
                    .Action($"PeacefulEnd.Parchment_JumpToChapter fishChapter{fishData.ItemData.ItemId}");
                bookmarkPanel.AddForeground("Image").Item(itemId).Scale(2).Alignment("Center")
                    .Position(-36, 16)
                    .Tint(Game1.player.fishCaught.ContainsKey(fishData.QualifiedItemId) ? "255 255 255 255" : "0 0 0 89")
                    .IgnoreCursor(true)
                    .AddFrameInPlace(condition: $"PeacefulEnd.Parchment_HasFlag bookmark{fishData.ItemData.ItemId}")
                    .FrameOffset(8, 0);
            }

            book.Set("Layout.MarginTop", "20");
            book.Set("Layout.MarginSpine", "8");

            book.Set("Appearance.TintColor", "200 200 100 255");
            book.Set("Appearance.TexturePath", "Assets/PeacefulEnd.Parchment.FishingLogbook/notebook");
            book.Set("Appearance.GrayscaleTexturePath", "Assets/PeacefulEnd.Parchment.FishingLogbook/notebookGrayscale");
            book.Set("Appearance.OpenFrameCount", 6);
            book.Set("Appearance.Offset", new Point(0, 5));

            book.Set("PageCurl.TexturePath", "Assets/PeacefulEnd.Parchment/curlPage2");
            book.Set("PageCurl.PreviousPageOffset", new Point(5, 105));
            book.Set("PageCurl.NextPageOffset", new Point(182, 105));

            // Build the cover
            book.AddOverlay("Banner").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(0, 64, 64, 32).Position(375, 125).Scale(3).Set("CapWidth", 29).Text(ModEntry.translation.Get("ui.cover.title")).Font("Dialogue").Condition(COVER_STATE_CONDITION);
            book.AddOverlay("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(0, 16, 14, 8).Position(350, 115).Scale(5).Origin(7, 4).Rotation(-45).Condition(COVER_STATE_CONDITION);
            book.AddOverlay("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(0, 16, 14, 8).Position(700, 200).Scale(5).Origin(7, 4).Rotation(-45).Condition(COVER_STATE_CONDITION);

            // Build the stickers
            book.AddOverlay("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(96, 0, 48, 16).Position(400, 250).Origin(24, 8).Rotation(20).Scale(6).Condition(COVER_STATE_CONDITION);

            foreach (var placement in _stickerPlacements)
            {
                book.AddOverlay("Image")
                    .Texture(placement.Sticker.TexturePath)
                    .TextureSource(0, 0, placement.Sticker.Size.X, placement.Sticker.Size.Y)
                    .Position((int)placement.Position.X, (int)placement.Position.Y)
                    .Scale(placement.Scale)
                    .Set("SpriteEffects", placement.IsFlipped ? "FlipHorizontally" : "None")
                    .Condition(COVER_STATE_CONDITION);
            }

            // Build the intro pages (first two seen on book open)
            var contentsPage = book.AddPage("recent-catches", "front");
            contentsPage.AddTitle(ModEntry.translation.Get("ui.page.recentCatches.title")).Alignment("Center").Spacing(32);
            foreach (var recentCatchInfo in RecentCatchHelper.GetRecentCatches(Game1.player).Reverse<RecentCatchInfo>().Take(7))
            {
                var recentCatchPanel = contentsPage.AddPanel().Alignment("Center").Margin(64, 0).Spacing(20);

                recentCatchPanel.AddBackground("Paragraph").Font("Small").Text($"•").Scale(1).Alignment("Left").VerticalAlignment("Center").Position(-25, 8);
                recentCatchPanel.AddChild("Image").Item(recentCatchInfo.Id).Scale(3);

                var catchText = recentCatchInfo.SizeInInches > 0 ? ModEntry.translation.Get("ui.recentCatch.withSize", new { itemName = $"[ItemName {recentCatchInfo.Id}]", size = recentCatchInfo.SizeInInches }).ToString() : $"[ItemName {recentCatchInfo.Id}]";
                recentCatchPanel.AddBackground("Paragraph").Font("Small").Text(catchText).Scale(1).Alignment("Left").VerticalAlignment("Center").Width(300).Position(75, 0);
            }

            var statsPage = book.AddPage("stats", "front");
            statsPage.AddTitle(ModEntry.translation.Get("ui.page.stats.title")).Alignment("Center");

            GetUniqueFishStats(Game1.player, collectableFishes, out int uniqueFishCaught, out int uniqueFishTotal);
            AddPanelWithTitle(statsPage, new Point(40, 100), ModEntry.translation.Get("ui.stats.uniqueFishCaught"), $"{uniqueFishCaught} / {uniqueFishTotal}", childPanelWidth: 50);
            AddPanelWithTitle(statsPage, new Point(-40, 100), ModEntry.translation.Get("ui.stats.totalFishCaught"), $"{Game1.player.stats.Get("FishCaught")}", alignment: "Right", childPanelWidth: 50);
            AddPanelWithTitle(statsPage, new Point(40, 300), ModEntry.translation.Get("ui.stats.treasuresFound"), $"{Game1.player.stats.Get("FishingTreasures")}", childPanelWidth: 50);
            AddPanelWithTitle(statsPage, new Point(-40, 300), ModEntry.translation.Get("ui.stats.fishingAttempts"), $"{Game1.player.stats.Get("TimesFished")}", alignment: "Right", childPanelWidth: 50);

            int gridColumns = 5;
            int gridRows = 5;
            int maxFishPerPage = gridColumns * gridRows;

            int fishPreviewPageCounter = 0;
            foreach (var pageFishes in collectableFishes.OrderBy(b => b.ItemData.ItemId).Chunk(maxFishPerPage).DefaultIfEmpty(Array.Empty<FishEntry>()))
            {
                var fishPreviewPage = book.AddPage($"fishPreview{fishPreviewPageCounter}", "front").Tag("Searchable");

                var pageNumber = fishPreviewPage.AddBackground("Paragraph").Font("Small").Text($"{fishPreviewPageCounter + 1}").Alignment("Right").VerticalAlignment("Bottom").Position(-16, 0);
                if (fishPreviewPageCounter is 0 || fishPreviewPageCounter % 2 == 0)
                {
                    pageNumber.Alignment("Left").Position(16, 0);
                    fishPreviewPage.AddTitle(ModEntry.translation.Get("ui.page.fish.title")).Alignment("Center");
                }

                BuildFishPreviewGrid(book, fishPreviewPage, pageFishes, gridColumns);
                fishPreviewPageCounter += 1;
            }

            // Create fish specific pages
            foreach (var fishData in collectableFishes)
            {
                bool itemUnlocked = Game1.player.fishCaught.ContainsKey(fishData.QualifiedItemId);

                FishCatchInfo info = FishCatchHelper.GetCatchInfo(fishData.QualifiedItemId, Game1.player);
                int amountCaught = info.TimesCaught;
                string size = FishCatchHelper.FormatBiggestCatch(info);

                var fishPageId = $"fishChapter{fishData.ItemData.ItemId}";
                var fishPageSummary = book.AddPage(fishPageId, fishPageId);

                var noSlotsBubble = fishPageSummary.AddForeground("Panel").WithId("noSlotsBubble").Position(10, 35)
                    .Lifetime(3f).FadeAfter(2f).IgnoreCursor()
                    .AddFrameInPlace(100f).FrameOffset(0, 2)
                    .AddFrameInPlace(100f).FrameOffset(0, 4)
                    .AddFrameInPlace(100f).FrameOffset(0, 5)
                    .AddFrameInPlace(100f).FrameOffset(0, 4)
                    .AddFrameInPlace(100f).FrameOffset(0, 2)
                    .AddFrameInPlace(100f).FrameOffset(0, 0);
                noSlotsBubble.AddChild("Banner").Text(ModEntry.translation.Get("ui.bookmark.noSlots")).Font("Small")
                    .Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(103, 133, 20, 20).Set("CapWidth", 7).Padding(8).Scale(2);
                noSlotsBubble.AddForeground("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(134, 152, 5, 5).Scale(2)
                    .Alignment("Center").VerticalAlignment("Bottom").Position(25, 8);

                fishPageSummary.AddForeground("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(96, 112, 16, 16).Scale(2).Alignment("Right").Position(-10, 5)
                    .Set("Description", ModEntry.translation.Get("ui.tooltip.bookmarkFish").ToString())
                    .AddFrame(112, 112, condition: IsBookmarked(fishData.QualifiedItemId))
                    .Action($"{TOGGLE_BOOKMARK} {fishData.QualifiedItemId}")
                    .Action("PeacefulEnd.Parchment_RefreshBook");

                fishPageSummary.AddTitle(GetFishDisplayName(Game1.player, fishData)).Alignment("Center").Spacing(36);
                var fishPanel = fishPageSummary.AddPanel().Spacing(32);
                fishPanel.AddBackground("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(48, 0, 16, 16).Scale(8).Alignment("Center").Position(0, -24);
                fishPanel.AddChild("Image").Item(fishData.QualifiedItemId).Tint(itemUnlocked ? "255 255 255 255" : "0 0 0 89").Set("DisplayName", "").Set("Description", "")
                    .Scale(5).Alignment("Center")
                    .Spacing(8);
                fishPageSummary.AddParagraph(fishData.ItemData.Description).Alignment("Left").Margin(24, 0).Spacing(12);
                fishPageSummary.AddDivider().Tint("234 200 178").Width(384).Sizing("Fixed").Set("Thickness", 4).Alignment("Center").Spacing(16);
                string statsText = ModEntry.translation.Get("ui.fish.amountCaught", new { amount = amountCaught }).ToString();

                // Crab pot entries and non-fish catches store sizes the game never reads, so omit both size lines rather than showing a max the player can never reach
                if (FishDataHelper.HasCatchSize(fishData))
                {
                    string largestSizeText = ModEntry.translation.Get("ui.fish.largestSize", new { size = string.IsNullOrEmpty(size) ? ModEntry.translation.Get("ui.fish.largestSizeNone").ToString() : size });
                    string maxSizeText = ModEntry.translation.Get("ui.fish.maxSize", new { size = fishData.FishData.MaxSize });
                    statsText = $"{statsText}\n{largestSizeText}\n{maxSizeText}";
                }

                fishPageSummary.AddHeading(ModEntry.translation.Get("ui.fish.statsHeading")).Alignment("Center").Margin(24, 0).Spacing(12);
                fishPageSummary.AddParagraph(statsText).Alignment("Left").Margin(24, 0).Spacing(12);
                fishPageSummary.AddDivider().Tint("234 200 178").Width(384).Sizing("Fixed").Set("Thickness", 4).Alignment("Center").Spacing(16);

                BuildFishDetailPages(book, fishPageId, fishData);
            }

            // Create the search page
            var searchPage = book.AddPage("searchPage", "searchChapter").Tag("Searchable");
            searchPage.AddTitle(ModEntry.translation.Get("ui.page.search.title")).Alignment("Center").Spacing(48);
            var grid = searchPage.AddGrid(64, 64, 5, rows: 5).CellSpacing(8, 8).Alignment("Center");
            foreach (var fishData in collectableFishes)
            {
                bool itemUnlocked = Game1.player.fishCaught.ContainsKey(fishData.QualifiedItemId);

                var fishPageId = $"fishChapter{fishData.ItemData.ItemId}";

                var fishPanel = grid.AddChild("Panel");
                fishPanel.AddBackground("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(48, 0, 16, 16).Scale(4).Alignment("Center").Position(0, -8);
                fishPanel.AddChild("Image").Item(fishData.QualifiedItemId).Tint(itemUnlocked ? "255 255 255 255" : "0 0 0 89").Set("DisplayName", GetFishDisplayName(Game1.player, fishData)).Set("Description", "")
                    .Scale(3).Alignment("Center")
                    .Origin(8, 8)
                    .AddHoverFrameInPlace(100, 1f).AddHoverFrameInPlace(100, 1.1f).AddHoverFrameInPlace(100, 1.15f).AddHoverFrameInPlace(100, 1.1f)
                    .Action($"PeacefulEnd.Parchment_JumpToChapter {fishPageId}")
                    .Action($"PeacefulEnd.Parchment_ClearInput search");
                fishPanel.Condition($"PeacefulEnd.Parchment_InputMatches search {fishData.DisplayName}");
            }

            // Create settings page
            var settingsPanelOverlay = book.AddOverlay("Panel").Position(553, 125)
                .Condition(READABLE_STATE_CONDITION)
                .Action("PeacefulEnd.Parchment_JumpToPageId fishPreview0");

            settingsPanelOverlay.AddChild("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(64, 64, 32, 17).Scale(4).Alignment("Center")
                .Set("Description", ModEntry.translation.Get("ui.tooltip.goToSettings").ToString())
                .Tint(nameof(Color.LightSteelBlue))
                .AddFrameInPlace()
                .FrameAction("PeacefulEnd.Parchment_ClearFlag settingBarHovered")
                .AddHoverFrame(64, 81)
                .HoverAction("PeacefulEnd.Parchment_SetFlag settingBarHovered")
                .Action("PeacefulEnd.Parchment_JumpToPageId settingsPage");
            settingsPanelOverlay.AddForeground("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(112, 64, 16, 16).Scale(2).Alignment("Center")
                .Position(-36, 16)
                .AddFrameInPlace(condition: "PeacefulEnd.Parchment_HasFlag settingBarHovered")
                .FrameOffset(8, 0);

            var settingsPage = book.AddPage("settingsPage", "settingsChapter");
            settingsPage.AddTitle(ModEntry.translation.Get("ui.page.settings.title")).Alignment("Center").Spacing(48);

            var forceFishNamePanel = settingsPage.AddPanel().Margin(32, 0).Spacing(24);
            forceFishNamePanel
                .AddChild("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(96, 80, 9, 9).Scale(4).Alignment("Left")
                .AddFrame(96, 89, condition: $"PeacefulEnd.Parchment_HasVariable {ModEntry.BOOK_ID} forceFishNames true")
                .Action($"PeacefulEnd.Parchment_ToggleVariable {ModEntry.BOOK_ID} forceFishNames")
                .Action("PeacefulEnd.Parchment_RefreshBook");
            forceFishNamePanel
                .AddForeground("Paragraph").Text(ModEntry.translation.Get("ui.settings.alwaysShowFishNames")).Alignment("Left").Position(50, 0);

            var startOnCoverPanel = settingsPage.AddPanel().Margin(32, 0).Spacing(24);
            startOnCoverPanel
                .AddChild("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(96, 80, 9, 9).Scale(4).Alignment("Left")
                .AddFrame(96, 89, condition: $"PeacefulEnd.Parchment_HasVariable {ModEntry.BOOK_ID} startOnCover true")
                .Action($"PeacefulEnd.Parchment_ToggleVariable {ModEntry.BOOK_ID} startOnCover")
                .Action("PeacefulEnd.Parchment_RefreshBook");
            startOnCoverPanel
                .AddForeground("Paragraph").Text(ModEntry.translation.Get("ui.settings.startOnCover")).Alignment("Left").Position(50, 0);

            var exitToCoverPanel = settingsPage.AddPanel().Margin(32, 0).Spacing(24);
            exitToCoverPanel
                .AddChild("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(96, 80, 9, 9).Scale(4).Alignment("Left")
                .AddFrame(96, 89, condition: $"PeacefulEnd.Parchment_HasVariable {ModEntry.BOOK_ID} exitToCover true")
                .Action($"PeacefulEnd.Parchment_ToggleVariable {ModEntry.BOOK_ID} exitToCover")
                .Action("PeacefulEnd.Parchment_RefreshBook");
            exitToCoverPanel
                .AddForeground("Paragraph").Text(ModEntry.translation.Get("ui.settings.exitToCover")).Alignment("Left").Position(50, 0);

            settingsPage.AddBanner(ModEntry.translation.Get("ui.settings.clearBookmarks")).Alignment("Center").Spacing(48).Scale(6).Font("Small").Set("CapWidth", 4)
                .Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(130, 131, 13, 9).Padding(8)
                .Action($"PeacefulEnd.Parchment_ClearVariable {ModEntry.BOOK_ID} bookmark1")
                .Action($"PeacefulEnd.Parchment_ClearVariable {ModEntry.BOOK_ID} bookmark2")
                .Action($"PeacefulEnd.Parchment_ClearVariable {ModEntry.BOOK_ID} bookmark3")
                .Action($"PeacefulEnd.Parchment_ClearVariable {ModEntry.BOOK_ID} bookmark4")
                .Action($"PeacefulEnd.Parchment_ClearVariable {ModEntry.BOOK_ID} bookmark5");

            return book;
        }

        /// <summary>
        /// Opens the registered logbook, which Parchment rebuilds on the way in when a catch has left it out of date.
        /// </summary>
        public void OpenBook()
        {
            if (_parchmentApi.HasBook(ModEntry.BOOK_ID) && _parchmentApi.TryOpenBook(ModEntry.BOOK_ID))
            {
                return;
            }

            if (Build().TryOpen(out string error) is false)
            {
                ModEntry.monitor.Log($"Couldn't open the logbook, because {error}.", LogLevel.Warn);
            }
        }

        /// <summary>
        /// Records that the book's contents have changed. Parchment rebuilds it on the way into the next opening, unless it is already open.
        /// </summary>
        public void QueueRefresh()
        {
            // The mark costs nothing, so a day of catches nobody reads costs no rebuilds at all
            if (_parchmentApi.HasBook(ModEntry.BOOK_ID) && _parchmentApi.TryMarkBookStale(ModEntry.BOOK_ID, out string error) is false)
            {
                ModEntry.monitor.Log($"Couldn't mark the logbook as stale, because {error}.", LogLevel.Warn);
            }

            // A reader with the book open needs it now, since the mark above only takes effect at the next opening
            if (IsBookOpen())
            {
                _ticksUntilRefresh = REFRESH_DELAY_TICKS;
            }
        }

        /// <summary>
        /// Records that the cover's stickers need laying out again, which happens on the next build.
        /// </summary>
        public void QueueStickerRefresh()
        {
            _areStickersStale = true;
        }

        public void CancelQueuedRefresh()
        {
            _ticksUntilRefresh = 0;
            _pendingSwap = null;
            _swapAttemptsRemaining = 0;
        }

        /// <summary>
        /// Drops everything held for the current save, so the next one starts from scratch.
        /// </summary>
        public void Reset()
        {
            CancelQueuedRefresh();

            _areStickersStale = false;
            _stickerPlacements = null;
        }

        public void UpdateQueuedRefresh()
        {
            // A swap refused while the book animates open is retried until it takes, rather than rebuilding again
            if (_pendingSwap is not null)
            {
                TrySwapPendingBook();
                return;
            }

            if (_ticksUntilRefresh <= 0)
            {
                return;
            }

            _ticksUntilRefresh -= 1;
            if (_ticksUntilRefresh > 0)
            {
                return;
            }

            RefreshBook();
        }

        public void RefreshBook()
        {
            CancelQueuedRefresh();

            IBookBuilder book = Build();

            if (book.TryRegister(out string registerError) is false)
            {
                ModEntry.monitor.Log($"Couldn't register the logbook, because {registerError}.", LogLevel.Warn);
                return;
            }

            if (book.TryRefresh(out string refreshError))
            {
                return;
            }

            // Nothing being open is the ordinary case here, so only a book which is open is worth waiting on
            if (IsBookOpen())
            {
                _pendingSwap = book;
                _swapAttemptsRemaining = SWAP_ATTEMPT_LIMIT;
                return;
            }

            ModEntry.monitor.Log($"Couldn't refresh the logbook, because {refreshError}.", LogLevel.Trace);
        }

        /// <summary>
        /// Retries a swap Parchment turned down, which it does while the book is mid-animation.
        /// </summary>
        private void TrySwapPendingBook()
        {
            _swapAttemptsRemaining -= 1;

            if (_pendingSwap.TryRefresh(out string refreshError))
            {
                CancelQueuedRefresh();
                return;
            }

            // The reader may have closed the book in the meantime, in which case there is nothing left to swap into
            if (_swapAttemptsRemaining > 0 && IsBookOpen())
            {
                return;
            }

            CancelQueuedRefresh();
            ModEntry.monitor.Log($"Couldn't refresh the logbook, because {refreshError}.", LogLevel.Trace);
        }

        /// <summary>
        /// Gets whether a Parchment book is currently on screen, which is any book rather than this one specifically.
        /// </summary>
        private bool IsBookOpen()
        {
            return _parchmentApi.TryGetBookBounds(out _);
        }

        private void BuildFishDetailPages(IBookBuilder book, string fishPageId, FishEntry fish)
        {
            // Ordering is left position-independent so the page reads the same wherever the player opens the book
            FishAvailability availability = FishAvailabilityHelper.Summarize(fish, preferCurrentLocation: false);

            // The text is worked out up front, since it both fills the pages and decides whether a recorded plan still applies
            string requirementsText = availability.HasRequirements ? BuildRequirementsText(availability) : string.Empty;
            string whenText = BuildWhenText(availability);
            List<string> rowTexts = availability.Rows.Select(row => BuildLocationRowText(row, availability)).ToList();
            List<string> footnotes = BuildFootnotes(availability);

            DetailPagePlanner planner = DetailPagePlanner.For(fish.QualifiedItemId, GetLayoutSignature(requirementsText, whenText, rowTexts, footnotes));

            var detailsPage = AddDetailPage(book, $"{fishPageId}_details", fishPageId);
            detailsPage.AddTitle(ModEntry.translation.Get("ui.page.details.title")).Alignment("Center").Spacing(36);

            // Only the genuinely fish with requirements go here, so an ordinary fish skips the heading entirely
            if (availability.HasRequirements)
            {
                detailsPage.AddHeading(ModEntry.translation.Get("ui.details.requirementsHeading")).Alignment("Center").Margin(0, 0).Spacing(12);
                detailsPage.AddParagraph(requirementsText).Alignment("Left").Margin(24, 0).Spacing(24);
            }

            // Time and weather come from Data/Fish, so they hold everywhere and belong in one block rather than on every row
            detailsPage.AddHeading(ModEntry.translation.Get("ui.details.whenHeading")).Alignment("Center").Margin(0, 0).Spacing(12);
            detailsPage.AddParagraph(whenText).Alignment("Left").Margin(24, 0).Spacing(24);

            // Go to next page if it has requirements and has locations
            bool addedPageTurnHint = false;
            if (availability.HasRequirements && availability.Rows.Count is not 0)
            {
                // Add page turn hint
                addedPageTurnHint = true;
                //AddRightSidePageTurnHint(detailsPage);

                detailsPage.AddHeading(ModEntry.translation.Get("ui.details.whereHeading")).Alignment("Center").Spacing(12);
                AddContinuationHint(detailsPage);

                detailsPage = AddDetailPage(book, $"{fishPageId}_detailsContinued", fishPageId);
            }
            detailsPage.AddHeading(ModEntry.translation.Get("ui.details.whereHeading")).Alignment("Center").Spacing(12);

            if (rowTexts.Count is 0)
            {
                string emptyText = fish.IsCollectionOnly ? ModEntry.translation.Get("ui.details.unlistedSpawn") : ModEntry.translation.Get("ui.details.notCatchable");
                detailsPage.AddParagraph(emptyText).Alignment("Left").Margin(24, 4).Spacing(12);
                return;
            }

            // Fill each page until Parchment says the next row won't fit, then take it back off and carry it over
            int detailPageIndex = 0;
            var currentPage = detailsPage;
            var pendingRows = new List<string>(rowTexts);
            var currentPageRows = new List<string>();

            while (pendingRows.Count > 0)
            {
                string row = pendingRows[0];
                pendingRows.RemoveAt(0);

                AddLocationRow(currentPage, row);

                if (planner.WouldOverflow(currentPage) is false)
                {
                    currentPageRows.Add(row);
                    continue;
                }

                // This row doesn't fit, so hand it back and close the page off
                currentPage.RemoveLast();
                pendingRows.Insert(0, row);

                AddContinuationHint(currentPage);

                // The hint is an element of its own, so keep giving rows back until it has room too
                while (planner.WouldOverflow(currentPage) && currentPageRows.Count > 0)
                {
                    // The hint first, then the row beneath it, since only the last element can be removed
                    currentPage.RemoveLast();
                    currentPage.RemoveLast();

                    pendingRows.Insert(0, currentPageRows[currentPageRows.Count - 1]);
                    currentPageRows.RemoveAt(currentPageRows.Count - 1);

                    AddContinuationHint(currentPage);
                }

                if (addedPageTurnHint is false)
                {
                    addedPageTurnHint = true;
                    //AddRightSidePageTurnHint(detailsPage);
                }

                detailPageIndex++;
                currentPage = AddDetailPage(book, $"{fishPageId}_details{detailPageIndex}", fishPageId);
                currentPage.AddHeading(ModEntry.translation.Get("ui.details.whereContinuedHeading")).Alignment("Center").Margin(24, 0).Spacing(12);
                currentPageRows.Clear();
            }

            AddDetailFootnotes(book, fishPageId, currentPage, footnotes, planner, detailPageIndex + 1);
        }

        /// <summary>
        /// Builds a value which identifies the text a fish's detail pages are laid out from, so a recorded plan is dropped once any of it changes
        /// </summary>
        private int GetLayoutSignature(string requirementsText, string whenText, IReadOnlyList<string> rowTexts, IReadOnlyList<string> footnotes)
        {
            int signature = CombineSignature(rowTexts.Count, requirementsText);
            signature = CombineSignature(signature, whenText);

            foreach (string rowText in rowTexts)
            {
                signature = CombineSignature(signature, rowText);
            }

            foreach (string footnote in footnotes)
            {
                signature = CombineSignature(signature, footnote);
            }

            return signature;
        }

        private int CombineSignature(int signature, string value)
        {
            return signature * 31 + Game1.hash.GetDeterministicHashCode(value ?? string.Empty);
        }

        private IPageBuilder AddDetailPage(IBookBuilder book, string pageId, string fishPageId)
        {
            var page = book.AddPage(pageId, fishPageId).Tag("Searchable");
            page.OnKeyPress("Escape, ControllerB").Action("PeacefulEnd.Parchment_JumpToPageId fishPreview0");

            return page;
        }

        private void AddRightSidePageTurnHint(IPageBuilder page)
        {
            string readyOnThisPage = $"PeacefulEnd.Parchment_CurrentBookState Ready, PeacefulEnd.Parchment_CurrentPageId {page.PageId}";

            page.AddBackground("Image").Texture("Assets/PeacefulEnd.Parchment/curlPage2").TextureSource(0, 0, 32, 32).Scale(5).Position(325, 425)
                .AddFrame(32 * 0, 0, 3500f, condition: readyOnThisPage)
                .AddFrame(32 * 1, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 2, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 3, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 4, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 5, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 6, 0, 200f, condition: readyOnThisPage)
                .AddFrame(32 * 5, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 4, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 3, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 2, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 1, 0, 100f, condition: readyOnThisPage)
                .AddFrame(32 * 0, 0, 600000f, condition: readyOnThisPage)
                .AddHoverFrame(0, 0)
                .Condition("PeacefulEnd.Parchment_CurrentBookState Ready");
        }

        private string BuildWhenText(FishAvailability availability)
        {
            string timeSummary = availability.TimeSummary ?? ModEntry.translation.Get("ui.details.anyTime").ToString();
            List<string> lines = new List<string>() { ModEntry.translation.Get("ui.details.time", new { time = timeSummary }) };

            // These are only shown when they actually restrict the player, so an unrestricted fish gets a one line block
            if (availability.WeatherSummary is not null)
            {
                lines.Add(ModEntry.translation.Get("ui.details.weather", new { weather = availability.WeatherSummary }));
            }

            return string.Join("\n", lines);
        }

        private string BuildRequirementsText(FishAvailability availability)
        {
            List<string> lines = new List<string>();

            if (availability.MinFishingLevel > 0)
            {
                // The fish wide figure is a floor, so say so rather than letting it read as the requirement everywhere
                string spread = availability.HasUniformFishingLevel ? string.Empty : ModEntry.translation.Get("ui.requirement.fishingLevelSpread").ToString();
                lines.Add(ModEntry.translation.Get("ui.requirement.fishingLevel", new { level = availability.MinFishingLevel, spread = spread }));
            }
            if (availability.RequiresMagicBaitEverywhere)
            {
                lines.Add(ModEntry.translation.Get("ui.requirement.magicBait"));
            }
            if (availability.CatchLimit > 0)
            {
                lines.Add(availability.CatchLimit is 1 ? ModEntry.translation.Get("ui.requirement.oncePerSave") : ModEntry.translation.Get("ui.requirement.catchLimit", new { limit = availability.CatchLimit }));
            }
            if (availability.IsBossFish)
            {
                lines.Add(ModEntry.translation.Get("ui.requirement.bossFish"));
            }

            return string.Join("\n", lines);
        }

        private string BuildLocationRowText(FishAvailabilityRow row, FishAvailability availability)
        {
            string location = FishAvailabilityHelper.FormatLocation(row);
            string seasons = FishAvailabilityHelper.FormatSeasons(row.Seasons) ?? ModEntry.translation.Get("ui.season.never").ToString();
            string gear = row.IsCrabPot ? ModEntry.translation.Get("ui.location.crabPot").ToString() : string.Empty;

            // Everywhere requirement is stated up top instead, so only flag Magic Bait when it gates some routes and not others
            bool flagMagicBait = row.RequiresMagicBait && availability.RequiresMagicBaitEverywhere is false;
            string marker = flagMagicBait ? "*" : row.HasExtraConditions ? "^" : string.Empty;

            // Only worth repeating the level when this spot is gated higher than the fish as a whole
            string level = row.MinFishingLevel > availability.MinFishingLevel ? ModEntry.translation.Get("ui.location.level", new { level = row.MinFishingLevel }).ToString() : string.Empty;

            return ModEntry.translation.Get("ui.location.row", new { location = location, gear = gear, seasons = seasons, level = level, marker = marker });
        }

        private void AddContinuationHint(IPageBuilder page)
        {
            page.AddParagraph(ModEntry.translation.Get("ui.details.continued")).Alignment("Left").Margin(24, 0).Spacing(8);
        }

        private void AddLocationRow(IPageBuilder page, string rowText)
        {
            page.AddParagraph(rowText).Alignment("Left").Margin(24, 0).Spacing(8);
        }

        private List<string> BuildFootnotes(FishAvailability availability)
        {
            List<string> footnotes = new List<string>();
            if (availability.Rows.Any(row => row.RequiresMagicBait) && availability.RequiresMagicBaitEverywhere is false)
            {
                footnotes.Add(ModEntry.translation.Get("ui.footnote.requiresMagicBait"));
            }
            if (availability.NeedsConditionFootnote)
            {
                footnotes.Add(ModEntry.translation.Get("ui.footnote.extraConditions"));
            }

            return footnotes;
        }

        private void AddDetailFootnotes(IBookBuilder book, string fishPageId, IPageBuilder page, List<string> footnotes, DetailPagePlanner planner, int spillPageIndex)
        {
            if (footnotes.Count is 0)
            {
                return;
            }

            page.AddDivider().Tint("234 200 178").Width(384).Sizing("Fixed").Set("Thickness", 4).Alignment("Center").Spacing(16);
            page.AddParagraph(string.Join("\n", footnotes)).Alignment("Left").Margin(24, 0).Spacing(12);

            if (planner.WouldOverflow(page) is false)
            {
                return;
            }

            // The footnotes ran off the bottom, so take them back off and give them a page of their own
            page.RemoveLast();
            page.RemoveLast();
            AddContinuationHint(page);

            var footnotePage = AddDetailPage(book, $"{fishPageId}_details{spillPageIndex}", fishPageId);
            footnotePage.AddParagraph(string.Join("\n", footnotes)).Alignment("Left").Margin(24, 0).Spacing(12);
        }

        private void BuildFishPreviewGrid(IBookBuilder book, IPageBuilder page, IReadOnlyList<FishEntry> fishes, int maxColumns)
        {
            int startingColumnX = 40;
            int startingRowY = 100;
            int cellSize = 75;

            for (int index = 0; index < fishes.Count; index++)
            {
                var fishEntry = fishes[index];
                var point = new Point(startingColumnX + index % maxColumns * cellSize, startingRowY + index / maxColumns * cellSize);

                AddCollectableItemWithBackground(book, page, point, fishEntry, Game1.player.fishCaught.ContainsKey(fishEntry.QualifiedItemId), "Left");
            }
        }

        private void AddCollectableItemWithBackground(IBookBuilder book, IPageBuilder page, Point point, FishEntry fishEntry, bool itemUnlocked, string alignment = "Left")
        {
            var fishPageId = $"fishChapter{fishEntry.ItemData.ItemId}";

            page.AddBackground("Image").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(48, 0, 16, 16).Scale(4).Alignment(alignment).Position(point.X, point.Y)
                .Condition($"ANY \"!PeacefulEnd.Parchment_HasInputText search\" \"PeacefulEnd.Parchment_InputMatches search {fishEntry.DisplayName}\"");
            page.AddBackground("Image").Item(fishEntry.QualifiedItemId).Tint(itemUnlocked ? "255 255 255 255" : "0 0 0 89").Set("DisplayName", GetFishDisplayName(Game1.player, fishEntry))
                .Set("Description", "").Scale(3).Alignment(alignment).Position(point.X + 7, point.Y + 8).Origin(8, 8)
                .AddHoverFrameInPlace(100, 1f).AddHoverFrameInPlace(100, 1.1f).AddHoverFrameInPlace(100, 1.15f).AddHoverFrameInPlace(100, 1.1f)
                .Action($"PeacefulEnd.Parchment_JumpToChapter {fishPageId}")
                .Condition($"ANY \"!PeacefulEnd.Parchment_HasInputText search\" \"PeacefulEnd.Parchment_InputMatches search {fishEntry.DisplayName}\"");
        }

        private void AddPanelWithTitle(IPageBuilder page, Point point, string panelTitle, string panelText, int parentPanelWidth = 150, int childPanelWidth = 25, string alignment = "Left")
        {
            var parentPanel = page.AddBackground("Panel").Width(parentPanelWidth).Sizing("Fixed").Padding(0).Alignment(alignment).Position(point.X, point.Y);
            parentPanel.AddChild("Paragraph").Text(panelTitle).Alignment("Center");

            var childPanel = parentPanel.AddChild("Panel").Texture(ModEntry.BOOK_DETAILS_ASSET).TextureSource(64, 32, 24, 24).Scale(3).Width(childPanelWidth).Sizing("Fixed").Padding(0).Alignment("Center");
            childPanel.AddChild("Paragraph").Text(panelText).Alignment("Center");
        }

        private void GetUniqueFishStats(Farmer who, IReadOnlyList<FishEntry> collectableFishes, out int uniqueFishCaught, out int uniqueFishTotal)
        {
            uniqueFishCaught = 0;
            uniqueFishTotal = collectableFishes.Count;

            foreach (var fishData in collectableFishes)
            {
                if (who.fishCaught.ContainsKey(fishData.QualifiedItemId))
                {
                    uniqueFishCaught += 1;
                }
            }
        }

        private bool ShouldShowFishName(Farmer who, string qualifiedFishId)
        {
            return who.fishCaught.ContainsKey(qualifiedFishId) || GameStateQuery.CheckConditions($"PeacefulEnd.Parchment_HasVariable {ModEntry.BOOK_ID} forceFishNames true");
        }

        /// <summary>
        /// Gets the fish's display name, falling back to a placeholder while it stays unidentified.
        /// </summary>
        private string GetFishDisplayName(Farmer who, FishEntry fish)
        {
            return ShouldShowFishName(who, fish.QualifiedItemId) ? fish.DisplayName : ModEntry.translation.Get("ui.fish.unknownName");
        }

        private string IsBookmarked(string qualifiedItemId)
        {
            return $"ANY {string.Join(" ", Enumerable.Range(1, 5).Select(slot => $"\"PeacefulEnd.Parchment_HasVariable {ModEntry.BOOK_ID} bookmark{slot} {qualifiedItemId}\""))}";
        }

        public bool ToggleBookmark(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string itemId, out error, name: "string itemId") is false)
            {
                return false;
            }

            int freeSlot = 0;
            for (int slot = 1; slot <= 5; slot++)
            {
                if (_parchmentApi.TryGetVariable(ModEntry.BOOK_ID, $"bookmark{slot}", out string current) is false)
                {
                    continue;
                }

                // Already bookmarked, so this press takes it off
                if (current.EqualsIgnoreCase(itemId))
                {
                    return _parchmentApi.TrySetVariable(ModEntry.BOOK_ID, $"bookmark{slot}", "none", out error);
                }

                if (freeSlot is 0 && current.EqualsIgnoreCase("none"))
                {
                    freeSlot = slot;
                }
            }

            if (freeSlot is 0)
            {
                TriggerActionManager.TryRunAction($"PeacefulEnd.Parchment_ShowElement noSlotsBubble", out _, out _);
                error = null;

                return true;
            }

            return _parchmentApi.TrySetVariable(ModEntry.BOOK_ID, $"bookmark{freeSlot}", itemId, out error);
        }
    }
}
