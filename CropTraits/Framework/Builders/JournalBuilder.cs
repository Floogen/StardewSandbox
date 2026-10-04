using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using Parchment.Framework.API;
using StardewModdingAPI;

namespace CropTraits.Framework.Builders
{
    /// <summary>
    /// Parchment book builder for tracking various trait discoveries.
    /// Note: The book is registered on save load and marked stale when discoveries change, so Parchment rebuilds it on the way into the next opening.
    /// </summary>
    public class JournalBuilder
    {
        public const string COVER_STATE_CONDITION = "ANY \"PeacefulEnd.Parchment_CurrentBookState Cover\" \"PeacefulEnd.Parchment_CurrentBookState Sliding\"";
        public const string FRONT_CHAPTER_ID = "front";
        public const string OVERVIEW_PAGE_ID = "overview";
        public const int REFRESH_DELAY_TICKS = 10;
        public const int SWAP_ATTEMPT_LIMIT = 60;

        public const string NOTEBOOK_TEXTURE_PATH = "Assets/PeacefulEnd.Parchment/notebook";
        public const string NOTEBOOK_GRAYSCALE_TEXTURE_PATH = "Assets/PeacefulEnd.Parchment/notebookGrayscale";
        public const string NOTEBOOK_PAGE_CURL_TEXTURE_PATH = "Assets/PeacefulEnd.Parchment/curlPage2";
        public const int NOTEBOOK_FRAME_WIDTH = 219;
        public const int NOTEBOOK_FRAME_HEIGHT = 143;
        public const int NOTEBOOK_OPEN_FRAME_COUNT = 6;
        public const int NOTEBOOK_TURN_FRAME_COUNT = 7;
        public const string NOTEBOOK_TINT_COLOR = "152 102 66";
        public static readonly Point NOTEBOOK_PREVIOUS_PAGE_OFFSET = new Point(5, 105);
        public static readonly Point NOTEBOOK_NEXT_PAGE_OFFSET = new Point(182, 105);

        public static readonly (int X, int Y) COVER_TITLE_POSITION = (375, 125);

        private readonly IParchmentApi _parchmentApi;
        private int _ticksUntilRefresh;
        private IBookBuilder _pendingSwap;
        private int _swapAttemptsRemaining;

        public JournalBuilder(IParchmentApi parchmentApi)
        {
            _parchmentApi = parchmentApi;
        }

        public IBookBuilder Build()
        {
            IBookBuilder book = _parchmentApi.CreateBook(CropTraits.JOURNAL_ID);
            book.OnRefresh(RefreshBook);

            ApplyNotebookAppearance(book);

            // Cover
            book.AddOverlay("Paragraph").Text(TranslationHelper.Get("journal.cover.title")).Font("Dialogue").Position(COVER_TITLE_POSITION.X, COVER_TITLE_POSITION.Y).Condition(COVER_STATE_CONDITION);

            BuildOverviewPage(book);

            return book;
        }

        public void ApplyNotebookAppearance(IBookBuilder book)
        {
            book.Set("Appearance.TexturePath", NOTEBOOK_TEXTURE_PATH);
            book.Set("Appearance.GrayscaleTexturePath", NOTEBOOK_GRAYSCALE_TEXTURE_PATH);
            book.Set("Appearance.FrameWidth", NOTEBOOK_FRAME_WIDTH);
            book.Set("Appearance.FrameHeight", NOTEBOOK_FRAME_HEIGHT);
            book.Set("Appearance.OpenFrameCount", NOTEBOOK_OPEN_FRAME_COUNT);
            book.Set("Appearance.TurnFrameCount", NOTEBOOK_TURN_FRAME_COUNT);
            book.Set("Appearance.TintColor", NOTEBOOK_TINT_COLOR);

            book.Set("PageCurl.TexturePath", NOTEBOOK_PAGE_CURL_TEXTURE_PATH);
            book.Set("PageCurl.PreviousPageOffset", NOTEBOOK_PREVIOUS_PAGE_OFFSET);
            book.Set("PageCurl.NextPageOffset", NOTEBOOK_NEXT_PAGE_OFFSET);
        }

        public void BuildOverviewPage(IBookBuilder book)
        {
            DiscoveryData discoveries = CropTraits.discoveryManager.Data;

            IPageBuilder overviewPage = book.AddPage(OVERVIEW_PAGE_ID, FRONT_CHAPTER_ID);
            overviewPage.AddTitle(TranslationHelper.Get("journal.page.overview.title")).Alignment("Center").Spacing(32);
            overviewPage.AddParagraph(TranslationHelper.Get("journal.overview.traits", new { count = discoveries.Traits.Count, total = CropTraits.traitManager.Traits.Count })).Alignment("Left").Margin(24, 0).Spacing(8);
            overviewPage.AddParagraph(TranslationHelper.Get("journal.overview.combinations", new { count = discoveries.Combinations.Count })).Alignment("Left").Margin(24, 0).Spacing(8);
            overviewPage.AddParagraph(TranslationHelper.Get("journal.overview.blends", new { count = discoveries.BlendJournal.Count })).Alignment("Left").Margin(24, 0).Spacing(8);
        }

        public void OpenBook()
        {
            if (_parchmentApi.HasBook(CropTraits.JOURNAL_ID) && _parchmentApi.TryOpenBook(CropTraits.JOURNAL_ID))
            {
                return;
            }

            if (Build().TryOpen(out string error) is false)
            {
                CropTraits.monitor.Log($"Couldn't open the journal: {error}.", LogLevel.Warn);
            }
        }

        public void QueueRefresh()
        {
            if (_parchmentApi.HasBook(CropTraits.JOURNAL_ID) && _parchmentApi.TryMarkBookStale(CropTraits.JOURNAL_ID, out string error) is false)
            {
                CropTraits.monitor.Log($"Couldn't mark the journal as stale: {error}.", LogLevel.Warn);
            }

            if (IsBookOpen())
            {
                _ticksUntilRefresh = REFRESH_DELAY_TICKS;
            }
        }

        public void CancelQueuedRefresh()
        {
            _ticksUntilRefresh = 0;
            _pendingSwap = null;
            _swapAttemptsRemaining = 0;
        }

        public void Reset()
        {
            CancelQueuedRefresh();
        }

        public void UpdateQueuedRefresh()
        {
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
                CropTraits.monitor.Log($"Couldn't register the journal: {registerError}.", LogLevel.Warn);
                return;
            }

            if (book.TryRefresh(out string refreshError))
            {
                return;
            }

            if (IsBookOpen())
            {
                _pendingSwap = book;
                _swapAttemptsRemaining = SWAP_ATTEMPT_LIMIT;
                return;
            }

            CropTraits.monitor.Log($"Couldn't refresh the journal: {refreshError}.", LogLevel.Trace);
        }

        private void TrySwapPendingBook()
        {
            _swapAttemptsRemaining -= 1;

            if (_pendingSwap.TryRefresh(out string refreshError))
            {
                CancelQueuedRefresh();
                return;
            }

            if (_swapAttemptsRemaining > 0 && IsBookOpen())
            {
                return;
            }

            CancelQueuedRefresh();
            CropTraits.monitor.Log($"Couldn't refresh the journal: {refreshError}.", LogLevel.Trace);
        }

        private bool IsBookOpen()
        {
            return _parchmentApi.TryGetBookBounds(out _);
        }
    }
}
