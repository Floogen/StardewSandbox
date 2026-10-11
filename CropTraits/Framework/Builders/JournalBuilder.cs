using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using Parchment.Framework.API;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Builders
{
    /// <summary>
    /// Parchment book builder for tracking various trait discoveries.
    /// Note: The book is registered on save load and marked stale when discoveries change, so Parchment rebuilds it on the way into the next opening.
    /// </summary>
    public class JournalBuilder
    {
        public const string COVER_STATE_CONDITION = "ANY \"PeacefulEnd.Parchment_CurrentBookState Cover\" \"PeacefulEnd.Parchment_CurrentBookState Sliding\"";
        public const string READABLE_STATE_CONDITION = "ANY \"PeacefulEnd.Parchment_CurrentBookState Ready\" \"PeacefulEnd.Parchment_CurrentBookState Turning\"";

        public const string FRONT_CHAPTER_ID = "front";
        public const string GETTING_STARTED_CHAPTER_ID = "gettingStarted";
        public const string SPOTTING_MUTATIONS_CHAPTER_ID = "spottingMutations";
        public const string SELECTIVE_BREEDING_CHAPTER_ID = "selectiveBreeding";
        public const string COMBINATION_TRAITS_CHAPTER_ID = "combinationTraits";
        public const string TRAITS_CHAPTER_ID = "traits";
        public const string TRAIT_CHAPTER_PREFIX = "trait_";
        public const string TRAIT_LINK_PREFIX = "trait_page_";
        public const string TRAITS_INDEX_LINK_ID = "traits_index_hyperlink";
        public const string CROPS_CHAPTER_ID = "crops";
        public const string CROPS_INDEX_LINK_ID = "crops_index_hyperlink";
        public const string SETTINGS_CHAPTER_ID = "settings";

        public const string START_ON_COVER_VARIABLE = "startOnCover";
        public const string EXIT_TO_COVER_VARIABLE = "exitToCover";
        public const string REVEAL_UNDISCOVERED_TRAITS_VARIABLE = "revealUndiscoveredTraits";

        public const string NEGATIVE_TRAIT_COLOR = "Red";
        public const int CONTENT_MARGIN = 24;
        public const int CROP_GRID_COLUMNS = 5;
        public const float CROP_GRID_ICON_SCALE = 3f;
        public const int TRAIT_INDEX_COLUMNS = 2;
        public const int CROSSBREED_SPACING = 8;
        public const float CROP_GRID_FRAME_SCALE = 4f;
        public static readonly Rectangle CROP_GRID_FRAME_SOURCE = new Rectangle(32, 32, 16, 16);

        public const int GUIDE_MARGIN = 12;
        public const int GUIDE_TEXT_SPACING = 24;
        public const string SEED_MAKER_QUALIFIED_ID = "(BC)25";

        public static readonly Rectangle BUTTON_SOURCE = new Rectangle(0, 0, 18, 18);
        public static readonly Rectangle BUTTON_HOVER_SOURCE = new Rectangle(0, 18, 18, 18);

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

            // Add variables for setting tracking
            book.AddVariable(START_ON_COVER_VARIABLE).Type("Boolean").Default("true").Scope("Global");
            book.AddVariable(EXIT_TO_COVER_VARIABLE).Type("Boolean").Default("true").Scope("Global");
            book.AddVariable(REVEAL_UNDISCOVERED_TRAITS_VARIABLE).Type("Boolean").Default("false").Scope("Global");

            book.OnRefresh(RefreshBook);

            // Setup the BookData
            book.Set("StartOnCover", GetBooleanVariable(START_ON_COVER_VARIABLE, true));
            book.Set("ExitToCover", GetBooleanVariable(EXIT_TO_COVER_VARIABLE, true));

            ApplyNotebookAppearance(book);

            // Cover
            book.AddOverlay("Paragraph").TextColor("255 223 96").Set("ShadowColor", "0 0 0 100").Scale(2).Text("[bold]Crop Traits[/bold]").Font("Dialogue").Alignment("Center").Position(0, 125).Condition(COVER_STATE_CONDITION);
            book.AddOverlay("Paragraph").TextColor("255 223 96").Set("ShadowColor", "0 0 0 100").Scale(2).Text("[bold]Guide[/bold]").Font("Dialogue").Alignment("Center").Position(0, 225).Condition(COVER_STATE_CONDITION);
            book.AddOverlay("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 336, 32, 32).Tint("255 255 255 125").Scale(6).Alignment("Center").VerticalAlignment("Center").Position(0, 125).Condition(COVER_STATE_CONDITION);
            //book.AddOverlay("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(29, 181, 19, 17).Scale(6).Alignment("Center").VerticalAlignment("Center").Position(135, 75).Condition(COVER_STATE_CONDITION);
            //book.AddOverlay("Image").Item(Game1.random.ChooseFrom(Game1.cropData.Values.Where(c => c is not null && c.Seasons.Contains(Game1.season)).ToList()).HarvestItemId).Scale(3).Alignment("Center").VerticalAlignment("Center").Position(145, 75).Condition(COVER_STATE_CONDITION);

            // Add bookmark to return to home page
            book.AddOverlay("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 180, 21, 20).Scale(4).Position(-24, 125)
            .Set("Description", "Go to home")
            .AddHoverFrame(0, 160)
            .Action("PeacefulEnd.Parchment_GoToStart")
            .Condition(READABLE_STATE_CONDITION);

            // Add escape handling to go back a page
            book.OnKeyPress("Escape, ControllerB").Action("PeacefulEnd.Parchment_GoBack").Condition($"PeacefulEnd.Parchment_CanGoBack, !PeacefulEnd.Parchment_IsFirstPage");

            // Get all relevant crops
            List<JournalCropEntry> crops = JournalDataHelper.GetCrops();

            // Chapter IDs come from the crop's place in the full list, since seed IDs from other mods may contain spaces that would split an action
            Dictionary<string, string> chapterIdsBySeedId = new();
            for (int index = 0; index < crops.Count; index++)
            {
                if (JournalDataHelper.IsDiscovered(crops[index].SeedId))
                {
                    chapterIdsBySeedId[crops[index].SeedId] = $"crop{index}";
                }
            }

            BuildOverviewPage(book, crops.Count, chapterIdsBySeedId.Count);

            // Handle links
            book.AddLink("getting_started_hyperlink").HoverEffect("underline").Action($"PeacefulEnd.Parchment_JumpToChapter {GETTING_STARTED_CHAPTER_ID}");
            book.AddLink("spotting_mutations_hyperlink").HoverEffect("underline").Action($"PeacefulEnd.Parchment_JumpToChapter {SPOTTING_MUTATIONS_CHAPTER_ID}");
            book.AddLink("selective_breeding_hyperlink").HoverEffect("underline").Action($"PeacefulEnd.Parchment_JumpToChapter {SELECTIVE_BREEDING_CHAPTER_ID}");
            book.AddLink("combination_traits_hyperlink").HoverEffect("underline").Action($"PeacefulEnd.Parchment_JumpToChapter {COMBINATION_TRAITS_CHAPTER_ID}");
            book.AddLink("discovered_traits_hyperlink").HoverEffect("underline").Action($"PeacefulEnd.Parchment_JumpToChapter {TRAITS_CHAPTER_ID}");
            book.AddLink("discovered_crops_hyperlink").HoverEffect("underline").Action($"PeacefulEnd.Parchment_JumpToChapter {CROPS_CHAPTER_ID}");
            book.AddLink("settings_hyperlink").HoverEffect("underline").Action($"PeacefulEnd.Parchment_JumpToChapter {SETTINGS_CHAPTER_ID}");

            book.AddLink("baseline").Tooltip(string.Empty, "The base, unmodified crop.");
            book.AddLink("trait").Tooltip(string.Empty, "A mutation of a baseline crop.\n\nMost crops have a small chance to gain a mutation after being planted.\n\nCrop traits can vary from helpful to harmful.");
            book.AddLink("combination_traits").Tooltip(string.Empty, "A special type of trait that appears when using the Seed Splicer with eligible traits from each parent.");
            book.AddLink("seed_splicer").Tooltip(string.Empty, "A machine that takes two crops of the same type to create seeds of a new germline.\n\nCan be used to combine traits or remove them.");

            book.AddLink("trait_seedless").Tooltip(string.Empty, "This trait prevents produce from being turned into seeds.\n\nIt does increase the sell price of the produce, however.");

            AddGettingStartedPages(book, GETTING_STARTED_CHAPTER_ID);
            AddSpottingMutationsPages(book, SPOTTING_MUTATIONS_CHAPTER_ID);
            AddSelectiveBreedingPages(book, SELECTIVE_BREEDING_CHAPTER_ID);
            AddCombinationTraitsPages(book, COMBINATION_TRAITS_CHAPTER_ID);

            // Links are registered up front so the trait and crop pages can point at each other
            HashSet<string> linkedTraitIds = RegisterTraitLinks(book);
            RegisterCropLinks(book, chapterIdsBySeedId);

            BuildTraitPages(book, linkedTraitIds);
            BuildCropGridPages(book, crops, chapterIdsBySeedId);

            foreach (JournalCropEntry crop in crops.Where(crop => chapterIdsBySeedId.ContainsKey(crop.SeedId)))
            {
                BuildCropChapter(book, crop, chapterIdsBySeedId[crop.SeedId], linkedTraitIds);
            }

            BuildSettingsPage(book, SETTINGS_CHAPTER_ID);

            return book;
        }

        public void ApplyNotebookAppearance(IBookBuilder book)
        {
            book.Set("Appearance.TexturePath", "Assets/PeacefulEnd.Parchment/notebook");
            book.Set("Appearance.GrayscaleTexturePath", "Assets/PeacefulEnd.Parchment/notebookGrayscale");
            book.Set("Appearance.FrameWidth", 219);
            book.Set("Appearance.FrameHeight", 143);
            book.Set("Appearance.OpenFrameCount", 6);
            book.Set("Appearance.TurnFrameCount", 7);
            book.Set("Appearance.TintColor", "152 102 66");

            book.Set("PageCurl.TexturePath", "Assets/PeacefulEnd.Parchment/curlPage2");
            book.Set("PageCurl.PreviousPageOffset", new Point(5, 105));
            book.Set("PageCurl.NextPageOffset", new Point(182, 105));

            book.Set("Layout.MarginTop", "20");
            book.Set("Layout.MarginBottom", "14");
            book.Set("Layout.MarginSpine", "10");
            book.Set("Layout.MarginOuter", "16");
        }

        public void BuildOverviewPage(IBookBuilder book, int cropCount, int discoveredCropCount)
        {
            DiscoveryData discoveries = CropTraits.discoveryManager.Data;

            IPageBuilder overviewPage = book.AddPage("overview", FRONT_CHAPTER_ID);
            overviewPage.AddTitle(TranslationHelper.Get("journal.page.overview.title")).Alignment("Center").Spacing(32);
            overviewPage.AddParagraph("- " + TranslationHelper.Get("journal.overview.traits", new { count = discoveries.Traits.Count, total = CropTraits.traitManager.Traits.Count })).Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(8);
            overviewPage.AddParagraph("- " + TranslationHelper.Get("journal.overview.crops", new { count = discoveredCropCount, total = cropCount })).Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(8);
            overviewPage.AddParagraph("- " + TranslationHelper.Get("journal.overview.combinations", new { count = discoveries.Combinations.Count })).Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(8);
            overviewPage.AddParagraph("- " + TranslationHelper.Get("journal.overview.blends", new { count = discoveries.BlendJournal.Count })).Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(8);

            overviewPage.AddHeading("-----").Font("Small").Alignment("Center").Spacing(24);
            overviewPage.AddHeading($"[bounce][bold]Hint:[/bold][/bounce] {GetRandomHint()}").Font("Small").Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(12);

            IPageBuilder contentPage = book.AddPage("contents", FRONT_CHAPTER_ID);
            contentPage.AddTitle("Contents").Alignment("Center").Spacing(32);
            contentPage.AddHeading("[link=getting_started_hyperlink]Getting Started[/link]").Font("Small").Alignment("Center").Spacing(0);
            contentPage.AddHeading("---").Font("Small").Alignment("Center").Spacing(12);
            contentPage.AddHeading("[link=spotting_mutations_hyperlink]Spotting Mutations[/link]").Font("Small").Alignment("Center").Spacing(12);
            contentPage.AddHeading("[link=selective_breeding_hyperlink]Selective Breeding[/link]").Font("Small").Alignment("Center").Spacing(12);
            contentPage.AddHeading("[link=combination_traits_hyperlink]Combination Traits[/link]").Font("Small").Alignment("Center").Spacing(12);
            contentPage.AddHeading("---").Font("Small").Alignment("Center").Spacing(12);
            contentPage.AddHeading("[link=discovered_traits_hyperlink]Discovered Traits[/link]").Font("Small").Alignment("Center").Spacing(12);
            contentPage.AddHeading("[link=discovered_crops_hyperlink]Discovered Crops[/link]").Font("Small").Alignment("Center").Spacing(12);
            contentPage.AddHeading("---").Font("Small").Alignment("Center").Spacing(12);
            contentPage.AddHeading("[link=settings_hyperlink]Settings[/link]").Font("Small").Alignment("Center").Spacing(12);
        }

        public string GetRandomHint()
        {
            var randomHints = new List<string>()
            {
                "You can go back a page by using [bold]Escape[/bold] key or the controller's [bold]B button[/bold]!",
                $"You can press [bold]{GetTraitViewKey()}[/bold] to show an icon over every crop with traits.\n\nHover over a crop to see its details, then press it again to hide the icons.",
                "The higher quality the parent crop, the more likely its [link=trait][underline]traits[/underline][/link] are inherited when using the [link=seed_splicer][underline]Seed Splicer[/underline][/link]!",
                "Planting adjacent crops that share the same [link=trait][underline]trait[/underline][/link] can be a helpful way to increase their tier!",
                "Each half of a [link=combination_traits][underline]combination traits[/underline][/link] must come from a different parent. A crop that has both parts won't fuse on its own."
            };

            return Game1.random.ChooseFrom(randomHints);
        }

        public IPageBuilder CreateNumberedPage(IBookBuilder book, string chapterId, int pageNumber, bool hideNumber=false)
        {
            IPageBuilder page = book.AddPage(GetPageId(chapterId, pageNumber), chapterId);

            if (hideNumber is false)
            {
                AddPageNumber(page, pageNumber);
            }

            return page;
        }

        public void AddPageNumber(IPageBuilder page, int pageIndex)
        {
            bool isRightPage = pageIndex % 2 == 0;
            IElementBuilder pageNumber = page.AddBackground("Paragraph").Font("Small").Text($"{pageIndex}").VerticalAlignment("Bottom");
            if (isRightPage is true)
            {
                pageNumber.Alignment("Right");
            }
        }

        public void AddGettingStartedPages(IBookBuilder book, string chapterId)
        {
            IPageBuilder page = CreateNumberedPage(book, chapterId, 1);

            page.AddTitle("Getting Started").Alignment("Center").Spacing(16);
            page.AddParagraph("Every crop you plant has a small chance to gain a [link=trait][underline]trait[/underline][/link] when it first sprouts.\n\nThese traits can make a crop grow faster, sell for more or even give an extra harvest. Some traits can even make crops survive outside their normal seasons!\n\nBe mindful that not every trait is helpful, however.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);

            page = CreateNumberedPage(book, chapterId, 2);
            page.AddParagraph("Mutagenic Fertilizer can be used to raise the chances of a trait appearing by allowing the crop to roll for mutations every night it's watered (until fully grown).").Spacing(176);
            page.AddBackground("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 48, 48, 32).Scale(4).Alignment("Center").VerticalAlignment("Center");
            page.AddBackground("Image").Item(CropTraits.MUTAGENIC_FERTILIZER_QUALIFIED_ID).Set("Description", TranslationHelper.Get("item.mutagenic_fertilizer.description") + "\n\nThis can be purchased from Pierre once you reach farming level 5.").Spacing(10).Scale(4).Alignment("Center").VerticalAlignment("Center").Position(0, -4);
            page.AddParagraph("Where you plant a crop also matters. When a plant mutates, it is much more likely to pick up a trait from adjacent crops [highlight]of the same type[/highlight].").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);

            page = CreateNumberedPage(book, chapterId, 3);
            page.AddHeading("Trait Details").Alignment("Center").Spacing(16);
            page.AddParagraph("Some traits have tiers, which strengthens their effect as the rank increases.\n\nMutations that occur for crops with existing traits may increase their rank, where applicable.\n\n[bounce][bold]Hint:[/bold][/bounce] Planting adjacent crops that share the same trait can be a helpful way to increase their tier!").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);

            page = CreateNumberedPage(book, chapterId, 4);
            page.AddParagraph("Seeds and crops can hold up to [highlight]three traits[/highlight].\n\nIf a crop already has three traits, it may result in one of them being replaced when a mutation occurs.\n\nSome traits, called [link=combination_traits][underline]combination traits[/underline][/link], can only be produced by using the [link=seed_splicer][underline]Seed Splicer[/underline][/link].\n\nThis notebook will automatically keep track of all discovered traits.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);

            page = CreateNumberedPage(book, chapterId, 5);
            page.AddHeading("Propagating Traits").Alignment("Center").Spacing(16);
            page.AddParagraph("To reliably propagate the same set of traits, you can utilize the Seed Maker. This machine is craftable at farming level 3.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(160);
            page.AddBackground("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 48, 48, 32).Scale(4).Alignment("Center").VerticalAlignment("Center");
            page.AddBackground("Image").Item(SEED_MAKER_QUALIFIED_ID).Spacing(10).Scale(4).Alignment("Center").VerticalAlignment("Center").Position(0, -24);
            page.AddParagraph("The Seed Maker will preserve the traits when creating new seeds, allowing for repeated planting of the same germline.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);

            page = CreateNumberedPage(book, chapterId, 6, hideNumber: true);
            page.AddHeading("Splicing Traits").Alignment("Center").Spacing(16);
            page.AddParagraph("Traits can be mixed together more reliably by using the Seed Splicer. This machine is craftable at farming level 9.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(160);
            page.AddBackground("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 48, 48, 32).Scale(4).Alignment("Center").VerticalAlignment("Center");
            page.AddBackground("Image").Item(CropTraits.SEED_SPLICER_QUALIFIED_ID).Spacing(10).Scale(4).Alignment("Center").VerticalAlignment("Center").Position(0, -24);
            page.AddParagraph("The Seed Splicer lets you mix traits together by crossbreeding two crops [highlight]of the same type[/highlight] but of different germlines.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);
        }

        public string GetTraitViewKey()
        {
            return CropTraits.modConfig.ToggleTraitViewKeybind.IsBound ? CropTraits.modConfig.ToggleTraitViewKeybind.ToString() : "the trait view key (set it in the config)";
        }

        public void AddSpottingMutationsPages(IBookBuilder book, string chapterId)
        {
            var cornSeeds = ItemRegistry.Create<StardewValley.Object>("(O)487");
            TraitModDataHelper.SetTraits(cornSeeds.modData, TraitSet.FromKey("Swift:2|Sweet:1"));

            IPageBuilder page = CreateNumberedPage(book, chapterId, 1);

            page.AddTitle("Spotting Mutations").Alignment("Center").Spacing(16);
            page.AddParagraph("Fully grown crops that have mutated will sparkle until they're harvested, so keep an eye out for any glimmers in your fields.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);
            page.AddBackground("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 80, 48, 48).Scale(6).Alignment("Center").VerticalAlignment("Center").Position(0, 128);

            page = CreateNumberedPage(book, chapterId, 2);
            page.AddParagraph("Seeds and produce that have traits are marked with a small green badge in the top-right corner. Hover over them to see their traits (negative ones are highlighted in red).").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(132);
            page.AddBackground("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 48, 48, 32).Scale(4).Alignment("Center").VerticalAlignment("Center");
            page.AddBackground("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 128, 48, 32).Set("DisplayName", cornSeeds.DisplayName).Set("Description", TraitTooltipHelper.GetDescriptionWithTraits(cornSeeds)).Scale(2).Alignment("Center").VerticalAlignment("Center").Position(0, -4);
            page.AddParagraph($"You can press [bold]{GetTraitViewKey()}[/bold] to show an icon over every crop with traits. Hover over a crop to see its details, then press it again to hide the icons.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);

            page = CreateNumberedPage(book, chapterId, 3);
            page.AddParagraph("Before a crop is fully grown, you will only see the traits it was planted with. Once it has finished growing, any new traits will be marked with \"[bounce][color=ForestGreen]New![/color][/bounce]\".").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0);
        }

        public void AddSelectiveBreedingPages(IBookBuilder book, string chapterId)
        {
            IPageBuilder page = CreateNumberedPage(book, chapterId, 1);

            page.AddTitle("Selective Breeding").Alignment("Center").Spacing(16);
            page.AddParagraph("Got a set of traits that you would like to keep growing? Put its produce into in a Seed Maker!").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(138);
            page.AddBackground("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 48, 48, 32).Scale(4).Alignment("Center").VerticalAlignment("Center").Position(0, -32);
            page.AddBackground("Image").Item(SEED_MAKER_QUALIFIED_ID).Spacing(10).Scale(4).Alignment("Center").VerticalAlignment("Center").Position(0, -56);
            page.AddParagraph("The Seed Maker is craftable starting from farming level 3.\n\n[bounce][bold]Note:[/bold][/bounce] Crops with the [link=trait_seedless][underline]Seedless[/underline][/link] trait are unable to be processed by the Seed Maker.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);

            page = CreateNumberedPage(book, chapterId, 2);
            page.AddParagraph("The Seed Splicer lets you mix traits by crossbreeding two crops [highlight]of the same type[/highlight] with different traits.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(112);
            page.AddBackground("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(0, 48, 48, 32).Scale(4).Alignment("Center").VerticalAlignment("Center").Position(0, -80);
            page.AddBackground("Image").Item(CropTraits.SEED_SPLICER_QUALIFIED_ID).Spacing(10).Scale(4).Alignment("Center").VerticalAlignment("Center").Position(0, -104);
            page.AddParagraph("The Seed Splicer is craftable starting from farming level 9.\n\nTo get started, put five of a crop in each slot and click Splice. After a few hours the seeds will be ready to collect.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);

            page = CreateNumberedPage(book, chapterId, 3);
            page.AddHeading("Mixing Traits").Alignment("Center").Spacing(16);
            page.AddParagraph("When inputting two crops with traits, they will crossbreed and combine their traits into new seeds.\n\nEvery splice rolls its own result, so the same pair can give different seeds.\n\nWhen both crops share a trait at the same tier, there is a small chance it grows stronger.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);

            page = CreateNumberedPage(book, chapterId, 4, hideNumber: true);
            page.AddParagraph("[bounce][bold]Hint:[/bold][/bounce] The higher quality the crop, the more likely its traits are inherited!").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);
            page.AddHeading("Diluting Traits").Alignment("Center").Spacing(16);
            page.AddParagraph("Got a trait you don't want? Splice the crop with a [link=baseline][underline]baseline[/underline][/link] crop of the same type.\n\nThis dilutes it, causing the removal of one of its traits or turning the seeds back to normal.\n\n[bounce][bold]Hint:[/bold][/bounce] The quality of both crops matter for preserving traits!").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(GUIDE_TEXT_SPACING);
        }

        public void AddCombinationTraitsPages(IBookBuilder book, string chapterId)
        {
            IPageBuilder page = CreateNumberedPage(book, chapterId, 1);

            page.AddTitle("Combination Traits").Alignment("Center").Spacing(16);
            page.AddParagraph("Some [link=trait][underline]traits[/underline][/link] can be mixed into a unique trait, called a [link=combination_traits][underline]combination trait[/underline][/link].\n\nWhen two crossbred crops each carry part of a required trait combination, there's a chance they combine into a single trait.\n\n[bounce][bold]Hint:[/bold][/bounce] Each half must come from a different parent. A crop that has both parts won't fuse on its own.").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(112);

            page = CreateNumberedPage(book, chapterId, 2, hideNumber: true);
            page.AddParagraph("A combination trait replaces the two traits that compose it. This can be helpful to bypass the three trait limit.\n\nA crop with a combination trait can still regain the traits it was made from. Their effects can stack with the combination's.\n\nSome combination traits can also turn a harmful trait into something useful!").Font("Small").Alignment("Left").Margin(GUIDE_MARGIN, 0).Spacing(112);
        }

        // The index lists only names, two columns per page, and each discovered (or revealed) trait opens its own page with the details
        // Traits get a page (and so a link) once discovered, or all of them when the reader turned on revealing undiscovered traits
        public HashSet<string> RegisterTraitLinks(IBookBuilder book)
        {
            DiscoveryData discoveries = CropTraits.discoveryManager.Data;
            bool isRevealingUndiscovered = GetBooleanVariable(REVEAL_UNDISCOVERED_TRAITS_VARIABLE, false);

            HashSet<string> linkedTraitIds = new();
            foreach (Trait trait in CropTraits.traitManager.Traits.Values.Where(trait => isRevealingUndiscovered || discoveries.Traits.ContainsKey(trait.Id)))
            {
                ConfigureJumpLink(book.AddLink($"{TRAIT_LINK_PREFIX}{trait.Id}"), $"{TRAIT_CHAPTER_PREFIX}{trait.Id}");
                linkedTraitIds.Add(trait.Id);
            }

            ConfigureJumpLink(book.AddLink(TRAITS_INDEX_LINK_ID), TRAITS_CHAPTER_ID);
            return linkedTraitIds;
        }

        // Shared by the book's links and the element links that repeat them, so both behave the same
        public ILinkBuilder ConfigureJumpLink(ILinkBuilder link, string chapterId)
        {
            return link.HoverEffect("underline").Action($"PeacefulEnd.Parchment_JumpToChapter {chapterId}");
        }

        public void RegisterCropLinks(IBookBuilder book, Dictionary<string, string> chapterIdsBySeedId)
        {
            foreach (string chapterId in chapterIdsBySeedId.Values)
            {
                ConfigureJumpLink(book.AddLink($"crop_page_{chapterId}"), chapterId);
            }

            ConfigureJumpLink(book.AddLink(CROPS_INDEX_LINK_ID), CROPS_CHAPTER_ID);
        }

        // The trait's text as a link to its page when it has one, in red when it's negative
        public string GetTraitLinkText(string traitId, string text, HashSet<string> linkedTraitIds)
        {
            bool isNegative = CropTraits.traitManager.TryGetTrait(traitId, out Trait trait) && trait.IsNegative;
            string coloredText = isNegative ? $"[color={NEGATIVE_TRAIT_COLOR}]{text}[/color]" : text;

            return linkedTraitIds.Contains(traitId) ? $"[link={TRAIT_LINK_PREFIX}{traitId}]{coloredText}[/link]" : coloredText;
        }

        public void BuildTraitPages(IBookBuilder book, HashSet<string> linkedTraitIds)
        {
            DiscoveryData discoveries = CropTraits.discoveryManager.Data;

            bool isRevealingUndiscovered = GetBooleanVariable(REVEAL_UNDISCOVERED_TRAITS_VARIABLE, false);
            List<Trait> orderedTraits = CropTraits.traitManager.Traits.Values
                .OrderByDescending(trait => isRevealingUndiscovered || discoveries.Traits.ContainsKey(trait.Id))
                .ThenBy(trait => CropTraits.traitManager.GetTraitName(trait.Id), StringComparer.CurrentCulture)
                .ToList();

            // First page displays 9 rows, the rest display 10 rows
            int firstPageSize = TRAIT_INDEX_COLUMNS * 9;
            List<Trait[]> traitPages = new List<Trait[]> { orderedTraits.Take(firstPageSize).ToArray() };
            traitPages.AddRange(orderedTraits.Skip(firstPageSize).Chunk(TRAIT_INDEX_COLUMNS * 10));

            for (int pageIndex = 0; pageIndex < traitPages.Count; pageIndex++)
            {
                bool isLastPage = pageIndex == traitPages.Count - 1;
                IPageBuilder page = CreateNumberedPage(book, TRAITS_CHAPTER_ID, pageIndex + 1, hideNumber: isLastPage || traitPages.Count <= 2);
                if (pageIndex == 0)
                {
                    page.AddTitle(TranslationHelper.Get("journal.page.traits.title")).Alignment("Center").Spacing(24);
                }

                IElementBuilder grid = page.AddGrid(212, 40, TRAIT_INDEX_COLUMNS).CellSpacing(8, 12).Alignment("Center");
                foreach (Trait trait in traitPages[pageIndex])
                {
                    AddTraitIndexEntry(grid, trait, linkedTraitIds);
                }
            }

            foreach (Trait trait in orderedTraits.Where(trait => linkedTraitIds.Contains(trait.Id)))
            {
                BuildTraitDetailPage(book, trait, discoveries, linkedTraitIds);
            }
        }

        // Undiscovered traits stay as plain "???" with nothing to click, unless the reader turned on revealing them
        public void AddTraitIndexEntry(IElementBuilder grid, Trait trait, HashSet<string> linkedTraitIds)
        {
            if (linkedTraitIds.Contains(trait.Id) is false)
            {
                grid.AddChild("Paragraph").Text("???").Font("Small").Alignment("Center");
                return;
            }

            grid.AddChild("Paragraph").Text(GetTraitLinkText(trait.Id, CropTraits.traitManager.GetTraitName(trait.Id), linkedTraitIds)).Font("Small").Alignment("Center");
        }

        public void BuildTraitDetailPage(IBookBuilder book, Trait trait, DiscoveryData discoveries, HashSet<string> linkedTraitIds)
        {
            string chapterId = $"{TRAIT_CHAPTER_PREFIX}{trait.Id}";
            int pageIndex = 0;
            IPageBuilder page = book.AddPage(GetPageId(chapterId, pageIndex), chapterId);

            IElementBuilder title = page.AddTitle(CropTraits.traitManager.GetTraitName(trait.Id)).Alignment("Center").Spacing(16);
            if (trait.IsNegative)
            {
                title.TextColor(NEGATIVE_TRAIT_COLOR);
            }

            page.AddParagraph(CropTraits.traitManager.GetTraitDescription(trait.Id)).Font("Small").Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(24);

            page = AddLineBlock(book, page, chapterId, ref pageIndex, "Details", GetTraitDetailLines(trait, discoveries, GetConflictingTraitIds(trait), linkedTraitIds), linkedTraitIds, 32);
            page = AddLineBlock(book, page, chapterId, ref pageIndex, "Effects", GetTraitEffectLines(trait), linkedTraitIds, 24);

            AddBackLink(book, page, chapterId, pageIndex, TRAITS_INDEX_LINK_ID, "Back to Traits");
        }

        // Each line is its own paragraph so the block can continue on the next page, with the heading kept beside its first line
        public IPageBuilder AddLineBlock(IBookBuilder book, IPageBuilder page, string chapterId, ref int pageIndex, string heading, List<string> lines, HashSet<string> linkedTraitIds, int blockSpacing)
        {
            for (int index = 0; index < lines.Count; index++)
            {
                string line = lines[index];
                bool isFirstLine = index == 0;
                int spacing = index == lines.Count - 1 ? blockSpacing : 2;

                page = AddWithOverflow(book, page, chapterId, ref pageIndex, entryPage =>
                {
                    int addedCount = 0;
                    if (isFirstLine)
                    {
                        entryPage.AddHeading(heading).Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(4);
                        addedCount += 1;
                    }

                    IElementBuilder paragraph = entryPage.AddParagraph(line).Font("Small").Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(spacing);
                    AddTraitLinks(paragraph, CropTraits.traitManager.Traits.Keys.Where(traitId => line.Contains($"[link={TRAIT_LINK_PREFIX}{traitId}]")), linkedTraitIds);
                    return addedCount + 1;
                });
            }

            return page;
        }

        // One line per tier (always all of them), followed by the trait's own notes
        public List<string> GetTraitEffectLines(Trait trait)
        {
            List<string> lines = new();
            for (int tier = 1; tier <= trait.MaxTier; tier++)
            {
                string tierDetails = trait.GetTierDetails(tier);
                if (string.IsNullOrEmpty(tierDetails))
                {
                    continue;
                }

                lines.Add(trait.MaxTier > 1 ? $"{TraitManager.ToRoman(tier)}: {tierDetails}" : tierDetails);
            }

            string notes = trait.GetNotes();
            if (string.IsNullOrEmpty(notes) is false)
            {
                lines.Add(notes);
            }

            return lines;
        }

        // Built from the trait's properties, so new traits get these lines without any text of their own
        public List<string> GetTraitDetailLines(Trait trait, DiscoveryData discoveries, List<string> conflictIds, HashSet<string> linkedTraitIds)
        {
            List<string> lines = new();

            SDate discoveryDate = null;
            if (discoveries.Traits.TryGetValue(trait.Id, out TraitDiscoveryData discovery) is false)
            {
                lines.Add(TranslationHelper.Get("journal.traits.not_discovered"));
            }
            else
            {
                discoveryDate = SDate.FromDaysSinceStart(discovery.FirstDiscoveredDay + 1);
                lines.Add($"Discovered: {discoveryDate.Season} {discoveryDate.Day} Year {discoveryDate.Year}");
            }

            if (trait is CombinationTrait combinationTrait)
            {
                lines.Add($"Fused from {GetTraitReferenceText(combinationTrait.FirstTraitId, linkedTraitIds)} and {GetTraitReferenceText(combinationTrait.SecondTraitId, linkedTraitIds)} via the Seed Splicer.");
            }
            else
            {
                lines.Add(trait.CanBeRolled ? $"Base rarity: {GetRarityText(trait.Rarity)}" : "Only appears through crossbreeding.");
            }

            if (trait.IsExclusive)
            {
                lines.Add("Can't share a crop with other traits.");
            }
            else if (conflictIds.Count > 0)
            {
                // Each conflict gets its own line, so a long list reads cleanly and can carry over to the next page
                lines.Add("\nCan't appear with the following:");
                for (int index = 0; index < conflictIds.Count; index++)
                {
                    bool isLast = index == conflictIds.Count - 1;
                    lines.Add($"- {GetTraitReferenceText(conflictIds[index], linkedTraitIds)}{(isLast ? "\n" : string.Empty)}");
                }
            }

            if (trait.BlocksPropagation)
            {
                lines.Add("Rejected by the Seed Maker and Seed Splicer.");
            }

            if (trait.IsPriceTrait)
            {
                lines.Add("Carries into artisan goods made from it, where any price change stacks with other price traits.");
            }

            if (discoveryDate is not null && trait.MaxTier > 1)
            {
                lines.Add(TranslationHelper.Get("journal.traits.tiers_seen", new { tiers = string.Join(", ", discovery.TiersSeen.OrderBy(tier => tier).Select(TraitManager.ToRoman)), max = TraitManager.ToRoman(trait.MaxTier) }));
            }
            return lines;
        }

        // Traits without a page (undiscovered while revealing is off) are scrambled, so conflicts and combination recipes don't spoil them
        public string GetTraitReferenceText(string traitId, HashSet<string> linkedTraitIds)
        {
            string name = CropTraits.traitManager.GetTraitName(traitId);
            if (linkedTraitIds.Contains(traitId) is false)
            {
                return $"????";
            }

            return GetTraitLinkText(traitId, name, linkedTraitIds);
        }

        public string GetRarityText(TraitRarity rarity)
        {
            string color = rarity switch
            {
                TraitRarity.Uncommon => "ForestGreen",
                TraitRarity.Rare => "RoyalBlue",
                _ => "DimGray"
            };

            return $"[color={color}]{rarity}[/color]";
        }

        // Conflicts are declared one way, so both directions are checked. Exclusive traits are covered by their own line instead
        public List<string> GetConflictingTraitIds(Trait trait)
        {
            return CropTraits.traitManager.Traits.Values
                .Where(other => other.Id != trait.Id && other.IsExclusive is false)
                .Where(other => trait.ConflictsWith.Contains(other.Id) || other.ConflictsWith.Contains(trait.Id))
                .OrderBy(other => CropTraits.traitManager.GetTraitName(other.Id), StringComparer.CurrentCulture)
                .Select(other => other.Id)
                .ToList();
        }

        public void BuildCropGridPages(IBookBuilder book, List<JournalCropEntry> listedCrops, Dictionary<string, string> chapterIdsBySeedId)
        {
            List<JournalCropEntry[]> cropPages = listedCrops.Chunk(CROP_GRID_COLUMNS * 5).DefaultIfEmpty(Array.Empty<JournalCropEntry>()).ToList();

            for (int pageIndex = 0; pageIndex < cropPages.Count; pageIndex++)
            {
                JournalCropEntry[] pageCrops = cropPages[pageIndex];
                bool isLastPage = pageIndex == cropPages.Count - 1;

                var page = CreateNumberedPage(book, CROPS_CHAPTER_ID, pageIndex + 1, hideNumber: isLastPage || cropPages.Count <= 2);
                if (pageIndex % 2 == 0)
                {
                    page.AddTitle(TranslationHelper.Get("journal.page.crops.title")).Alignment("Center").Spacing(24);
                }

                if (pageCrops.Length == 0)
                {
                    page.AddParagraph(TranslationHelper.Get("journal.crops.empty")).Alignment("Center").Margin(CONTENT_MARGIN, CONTENT_MARGIN);
                }
                else
                {
                    for (int index = 0; index < pageCrops.Length; index++)
                    {
                        AddCropGridCell(page, pageCrops[index], index, chapterIdsBySeedId);
                    }
                }
            }
        }

        public void AddCropGridCell(IPageBuilder page, JournalCropEntry crop, int index, Dictionary<string, string> chapterIdsBySeedId)
        {
            int cellStride = 64 + 8;
            int column = index % CROP_GRID_COLUMNS;
            int row = index / CROP_GRID_COLUMNS;

            int offsetX = (column - (CROP_GRID_COLUMNS / 2)) * cellStride;
            int cellTopY = 100 + (row * cellStride);
            int iconOffsetY = (int)((CROP_GRID_FRAME_SOURCE.Height * CROP_GRID_FRAME_SCALE) - (CROP_GRID_FRAME_SOURCE.Height * CROP_GRID_ICON_SCALE)) / 2;

            page.AddBackground("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(CROP_GRID_FRAME_SOURCE.X, CROP_GRID_FRAME_SOURCE.Y, CROP_GRID_FRAME_SOURCE.Width, CROP_GRID_FRAME_SOURCE.Height)
                .Scale(CROP_GRID_FRAME_SCALE).Alignment("Center").Position(offsetX, cellTopY);

            bool isDiscovered = chapterIdsBySeedId.TryGetValue(crop.SeedId, out string chapterId);
            IElementBuilder icon = page.AddBackground("Image").Item(crop.HarvestItemId).Scale(CROP_GRID_ICON_SCALE).Alignment("Center").Position(offsetX, cellTopY + iconOffsetY)
                .Tint(isDiscovered ? "255 255 255 255" : "0 0 0 89")
                .Set("DisplayName", crop.DisplayName)
                .Set("Description", isDiscovered ? string.Empty : TranslationHelper.Get("journal.crops.undiscovered"));

            if (isDiscovered)
            {
                icon.Action($"PeacefulEnd.Parchment_JumpToChapter {chapterId}");
            }
        }

        public void BuildCropChapter(IBookBuilder book, JournalCropEntry crop, string chapterId, HashSet<string> linkedTraitIds)
        {
            int pageIndex = 0;
            IPageBuilder page = book.AddPage(GetPageId(chapterId, pageIndex), chapterId);
            page.AddTitle(crop.DisplayName).Alignment("Center").Spacing(16);
            page.AddItemImage(crop.HarvestItemId).Scale(4f).Alignment("Center").Spacing(16);

            page.AddHeading(TranslationHelper.Get("journal.crop.traits_heading")).Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(4);
            IElementBuilder traitsParagraph = page.AddParagraph(GetCropTraitsText(crop.SeedId, linkedTraitIds)).Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(24);

            AddTraitLinks(traitsParagraph, JournalDataHelper.GetCropRecord(crop.SeedId).TraitTiers.Keys, linkedTraitIds);

            page = AddWithOverflow(book, page, chapterId, ref pageIndex, entryPage =>
            {
                entryPage.AddHeading(TranslationHelper.Get("journal.crop.crossbreeds_heading")).Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(8);
                return 1;
            });

            List<BlendJournalEntryData> crossbreeds = JournalDataHelper.GetCrossbreeds(crop.SeedId);
            if (crossbreeds.Count == 0)
            {
                page = AddWithOverflow(book, page, chapterId, ref pageIndex, entryPage =>
                {
                    entryPage.AddParagraph(TranslationHelper.Get("journal.crop.none")).Alignment("Left").Margin(CONTENT_MARGIN, 0);
                    return 1;
                });
            }

            for (int index = 0; index < crossbreeds.Count; index++)
            {
                BlendJournalEntryData crossbreed = crossbreeds[index];
                bool isLastEntry = index == crossbreeds.Count - 1;

                page = AddWithOverflow(book, page, chapterId, ref pageIndex, entryPage => AddCrossbreedEntry(entryPage, crossbreed, isLastEntry, linkedTraitIds));
            }

            AddBackLink(book, page, chapterId, pageIndex, CROPS_INDEX_LINK_ID, "Back to Crops");
        }

        // Returns how many elements were added, so the whole entry (and its divider) moves to the next page when it doesn't fit
        public int AddCrossbreedEntry(IPageBuilder page, BlendJournalEntryData crossbreed, bool isLastEntry, HashSet<string> linkedTraitIds)
        {
            int addedCount = 0;

            // Entries from before the day was recorded have 0, so they skip the date
            if (crossbreed.FirstSplicedDay > 0)
            {
                string date = SDate.FromDaysSinceStart(crossbreed.FirstSplicedDay + 1).ToLocaleString();
                page.AddParagraph(date).Font("Small").TextColor("DimGray").Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(2);
                addedCount += 1;
            }

            IElementBuilder entryParagraph = page.AddParagraph(FormatCrossbreed(crossbreed, linkedTraitIds)).Font("Small").Alignment("Left").Margin(CONTENT_MARGIN, 0).Spacing(CROSSBREED_SPACING);
            AddTraitLinks(entryParagraph, GetCrossbreedTraitIds(crossbreed), linkedTraitIds);
            addedCount += 1;

            if (isLastEntry)
            {
                return addedCount;
            }

            page.AddDivider().Tint("234 200 178").Width(384).Sizing("Fixed").Set("Thickness", 4).Alignment("Center").Spacing(CROSSBREED_SPACING);
            return addedCount + 1;
        }

        public string GetCropTraitsText(string seedId, HashSet<string> linkedTraitIds)
        {
            CropDiscoveryData record = JournalDataHelper.GetCropRecord(seedId);
            if (record.TraitTiers.Count == 0)
            {
                return TranslationHelper.Get("journal.crop.none");
            }

            IEnumerable<string> traitLabels = record.TraitTiers
                .OrderBy(trait => CropTraits.traitManager.GetTraitName(trait.Key), StringComparer.CurrentCulture)
                .Select(trait => FormatTraitTiers(trait.Key, trait.Value, linkedTraitIds));

            return string.Join(", ", traitLabels);
        }

        // The tiers sit in parentheses inside the link, so "Swift (I, II)" is one link
        public string FormatTraitTiers(string traitId, IEnumerable<int> tiers, HashSet<string> linkedTraitIds)
        {
            string name = CropTraits.traitManager.GetTraitName(traitId);
            if (CropTraits.traitManager.TryGetTrait(traitId, out Trait trait) && trait.MaxTier <= 1)
            {
                return GetTraitLinkText(traitId, name, linkedTraitIds);
            }

            return GetTraitLinkText(traitId, $"{name} ({string.Join(", ", tiers.OrderBy(tier => tier).Select(TraitManager.ToRoman))})", linkedTraitIds);
        }

        // Pinned to the bottom of the chapter's last page, or of a new page when the content already reaches the bottom
        public void AddBackLink(IBookBuilder book, IPageBuilder page, string chapterId, int pageIndex, string linkId, string text)
        {
            IPageBuilder linkPage = page;
            if (page.GetRemainingHeight() < 48)
            {
                linkPage = book.AddPage(GetPageId(chapterId, pageIndex + 1), chapterId);
            }

            linkPage.AddBackground("Paragraph").Text($"[link={linkId}]{text}[/link]").Font("Small").Alignment("Center").VerticalAlignment("Bottom");
        }

        public string FormatCrossbreed(BlendJournalEntryData crossbreed, HashSet<string> linkedTraitIds)
        {
            BlendParent.TryParseSortKey(crossbreed.FirstParentKey, out TraitSet firstTraits, out int firstQuality);
            BlendParent.TryParseSortKey(crossbreed.SecondParentKey, out TraitSet secondTraits, out int secondQuality);

            return TranslationHelper.Get("journal.crossbreed.entry", new
            {
                first = GetTraitSetLinkText(firstTraits, linkedTraitIds),
                firstQuality = JournalDataHelper.GetQualityName(firstQuality),
                second = GetTraitSetLinkText(secondTraits, linkedTraitIds),
                secondQuality = JournalDataHelper.GetQualityName(secondQuality),
                results = string.Join("\n", crossbreed.ResultTraitKeys.Select(resultTraitKey => "- " + GetTraitSetLinkText(TraitSet.FromKey(resultTraitKey), linkedTraitIds)))
            });
        }

        // Each trait's tier label ("Swift II") is its own link
        public string GetTraitSetLinkText(TraitSet traits, HashSet<string> linkedTraitIds)
        {
            IEnumerable<string> traitLabels = traits.Entries
                .OrderBy(trait => CropTraits.traitManager.GetTraitName(trait.Key), StringComparer.CurrentCulture)
                .Select(trait => GetTraitLinkText(trait.Key, CropTraits.traitManager.GetTierLabel(trait.Key, trait.Value), linkedTraitIds));

            return string.Join(", ", traitLabels);
        }

        public IEnumerable<string> GetCrossbreedTraitIds(BlendJournalEntryData crossbreed)
        {
            BlendParent.TryParseSortKey(crossbreed.FirstParentKey, out TraitSet firstTraits, out _);
            BlendParent.TryParseSortKey(crossbreed.SecondParentKey, out TraitSet secondTraits, out _);

            return firstTraits.Ids.Concat(secondTraits.Ids).Concat(crossbreed.ResultTraitKeys.SelectMany(resultTraitKey => TraitSet.FromKey(resultTraitKey).Ids));
        }

        // Measuring for overflow sees only an element's own links, so the trait links are repeated on the element itself
        public void AddTraitLinks(IElementBuilder element, IEnumerable<string> traitIds, HashSet<string> linkedTraitIds)
        {
            foreach (string traitId in traitIds.Distinct().Where(linkedTraitIds.Contains))
            {
                ConfigureJumpLink(element.AddLink($"{TRAIT_LINK_PREFIX}{traitId}"), $"{TRAIT_CHAPTER_PREFIX}{traitId}");
            }
        }

        public void BuildSettingsPage(IBookBuilder book, string chapterId)
        {
            IPageBuilder page = book.AddPage(GetPageId(chapterId, 0), chapterId);
            page.AddTitle(TranslationHelper.Get("journal.page.settings.title")).Alignment("Center").Spacing(32);

            AddSettingToggle(page, REVEAL_UNDISCOVERED_TRAITS_VARIABLE, TranslationHelper.Get("journal.settings.reveal_traits"));
            AddSettingToggle(page, START_ON_COVER_VARIABLE, TranslationHelper.Get("journal.settings.start_on_cover"));
            AddSettingToggle(page, EXIT_TO_COVER_VARIABLE, TranslationHelper.Get("journal.settings.exit_to_cover"));
        }

        public void AddSettingToggle(IPageBuilder page, string variableId, string label)
        {
            var settingPanel = page.AddPanel().Margin(32, 0).Spacing(24);
            settingPanel
                .AddChild("Image").Texture(AssetManager.SPRITE_SHEET_TEXTURE_PATH).TextureSource(32, 160, 9, 9).Scale(4).Alignment("Left")
                .AddFrame(32, 169, condition: $"PeacefulEnd.Parchment_HasVariable {CropTraits.JOURNAL_ID} {variableId} true")
                .IgnoreCursor();
            settingPanel
                .AddForeground("Paragraph").Text(label).Alignment("Left").Position(50, 0);
            settingPanel
                .Action($"PeacefulEnd.Parchment_ToggleVariable {CropTraits.JOURNAL_ID} {variableId}")
                .Action("PeacefulEnd.Parchment_RefreshBook");

        }

        public IElementBuilder AddButton(IPageBuilder page, string text, string action)
        {
            return page.AddButton(text, action)
                .Texture("Assets/PeacefulEnd.Parchment/button1").TextureSource(BUTTON_SOURCE.X, BUTTON_SOURCE.Y, BUTTON_SOURCE.Width, BUTTON_SOURCE.Height)
                .HoverTextureSource(BUTTON_HOVER_SOURCE.X, BUTTON_HOVER_SOURCE.Y, BUTTON_HOVER_SOURCE.Width, BUTTON_HOVER_SOURCE.Height)
                .Font("Small").Scale(1).Padding(2).Alignment("Center").Spacing(6);
        }

        public IPageBuilder AddWithOverflow(IBookBuilder book, IPageBuilder page, string chapterId, ref int pageIndex, Func<IPageBuilder, int> addEntry)
        {
            int addedCount = addEntry(page);
            if (page.WouldOverflow() is false)
            {
                return page;
            }

            for (int index = 0; index < addedCount; index++)
            {
                page.RemoveLast();
            }

            pageIndex += 1;
            IPageBuilder nextPage = book.AddPage(GetPageId(chapterId, pageIndex), chapterId);
            addEntry(nextPage);
            return nextPage;
        }

        public string GetPageId(string chapterId, int pageIndex)
        {
            return $"{chapterId}_page{pageIndex}";
        }

        public bool GetBooleanVariable(string variableId, bool defaultValue)
        {
            if (_parchmentApi.TryGetVariable(CropTraits.JOURNAL_ID, variableId, out string value) && bool.TryParse(value, out bool isEnabled))
            {
                return isEnabled;
            }

            return defaultValue;
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
                _ticksUntilRefresh = 10;
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
                _swapAttemptsRemaining = 60;
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
