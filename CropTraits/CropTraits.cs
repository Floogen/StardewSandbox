using CropTraits.Framework.Builders;
using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Patches;
using CropTraits.Framework.Patches.Objects;
using CropTraits.Framework.Patches.TerrainFeatures;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using Parchment.Framework.API;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits
{
    public class CropTraits : Mod
    {
        // Object IDs
        public const string SEED_SPLICER_ID = "PeacefulEnd.CropTraits_SeedSplicer";
        public const string SEED_SPLICER_QUALIFIED_ID = "(BC)" + SEED_SPLICER_ID;
        public const string MUTAGENIC_FERTILIZER_ID = "PeacefulEnd.CropTraits_MutagenicFertilizer";
        public const string MUTAGENIC_FERTILIZER_QUALIFIED_ID = "(O)" + MUTAGENIC_FERTILIZER_ID;
        public const string JOURNAL_ID = "PeacefulEnd.CropTraits_Journal";

        // Mod IDs
        public const string PARCHMENT_ID = "PeacefulEnd.Parchment.Core";

        // ModData keys
        public const string TRAITS_MOD_DATA_KEY = "PeacefulEnd.CropTraits/Traits"; // Trait key on seeds, produce, artisan goods and HoeDirt
        public const string SEED_TRAITS_MOD_DATA_KEY = "PeacefulEnd.CropTraits/SeedTraits"; // Seed's original traits on HoeDirt, shown until the crop reaches maturity
        public const string SEED_ID_MOD_DATA_KEY = "PeacefulEnd.CropTraits/SeedId"; // Seed ID that mutated produce was grown from
        public const string PLANTER_ID_MOD_DATA_KEY = "PeacefulEnd.CropTraits/PlanterId"; // Farmer who planted the crop on HoeDirt, so growth rebuilds use their professions
        public const string SPLICE_START_DAY_MOD_DATA_KEY = "PeacefulEnd.CropTraits/SpliceStartDay"; // Total day a running splice started, on the Seed Splicer
        public const string SPLICE_START_TIME_MOD_DATA_KEY = "PeacefulEnd.CropTraits/SpliceStartTime"; // Time of day a running splice started, on the Seed Splicer
        public const string SPLICE_UPGRADED_TRAITS_MOD_DATA_KEY = "PeacefulEnd.CropTraits/SpliceUpgradedTraits"; // Traits that grew stronger in the held seeds, on the Seed Splicer

        // Shared static helpers
        public static IManifest manifest;
        public static IMonitor monitor;
        public static IModHelper modHelper;

        // Config
        public static ModConfig modConfig;

        // Builders
        public static JournalBuilder journalBuilder;

        // Managers
        public static AssetManager assetManager;
        public static BlendManager blendManager;
        public static CropLightManager cropLightManager;
        public static DiscoveryManager discoveryManager;
        public static HarvestManager harvestManager;
        public static MutationManager mutationManager;
        public static MutationSparkleManager mutationSparkleManager;
        public static NightlyGrowthManager nightlyGrowthManager;
        public static RecipeManager recipeManager;
        public static TraitManager traitManager;
        public static TraitViewManager traitViewManager;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor and helper
            manifest = ModManifest;
            monitor = Monitor;
            modHelper = helper;

            // Load the config
            modConfig = helper.ReadConfig<ModConfig>();

            // Create managers
            assetManager = new AssetManager(monitor, helper);
            blendManager = new BlendManager(monitor, helper);
            cropLightManager = new CropLightManager(monitor, helper);
            discoveryManager = new DiscoveryManager(monitor, helper);
            harvestManager = new HarvestManager(monitor, helper);
            mutationManager = new MutationManager(monitor, helper);
            mutationSparkleManager = new MutationSparkleManager(monitor, helper);
            nightlyGrowthManager = new NightlyGrowthManager(monitor, helper);
            recipeManager = new RecipeManager(monitor, helper);
            traitManager = new TraitManager(monitor, helper);
            traitViewManager = new TraitViewManager(monitor, helper);

            try
            {
                var harmony = new Harmony(ModManifest.UniqueID);

                // Apply patches
                new HoeDirtPatch(monitor, modHelper).Apply(harmony);
                new CropPatch(monitor, modHelper).Apply(harmony);
                new GameLocationPatch(monitor, modHelper).Apply(harmony);
                new FarmPatch(monitor, modHelper).Apply(harmony);
                new GiantCropPatch(monitor, modHelper).Apply(harmony);
                new ItemRegistryPatch(monitor, modHelper).Apply(harmony);
                new ColoredObjectPatch(monitor, modHelper).Apply(harmony);
                new ObjectPatch(monitor, modHelper).Apply(harmony);
                new IndoorPotPatch(monitor, modHelper).Apply(harmony);
                new ItemPatch(monitor, modHelper).Apply(harmony);
                new MachineDataUtilityPatch(monitor, modHelper).Apply(harmony);
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Hook into the required events
            helper.Events.Content.AssetRequested += assetManager.OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
            helper.Events.GameLoop.DayEnding += (sender, args) => mutationManager.ResolveAllMutationRolls();
            helper.Events.GameLoop.DayEnding += (sender, args) => nightlyGrowthManager.PlanAllNightlyEffects();
            helper.Events.GameLoop.DayStarted += (sender, args) => SpliceHelper.UpdateAllReadyStates();
            helper.Events.GameLoop.DayStarted += (sender, args) => nightlyGrowthManager.Reset();
            helper.Events.GameLoop.DayStarted += (sender, args) => blendManager.ResetSpliceAttempts();
            helper.Events.GameLoop.TimeChanged += (sender, args) => SpliceHelper.UpdateAllReadyStates();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += (sender, args) => discoveryManager.Load();
            helper.Events.GameLoop.SaveLoaded += (sender, args) => journalBuilder?.RefreshBook();
            helper.Events.GameLoop.SaveLoaded += (sender, args) => recipeManager.LearnMissingRecipes();
            helper.Events.GameLoop.Saving += (sender, args) => discoveryManager.Save();
            helper.Events.GameLoop.ReturnedToTitle += (sender, args) => discoveryManager.Reset();
            helper.Events.GameLoop.ReturnedToTitle += (sender, args) => traitViewManager.Reset();
            helper.Events.GameLoop.ReturnedToTitle += (sender, args) => journalBuilder?.Reset();
            helper.Events.GameLoop.ReturnedToTitle += (sender, args) => nightlyGrowthManager.Reset();
            helper.Events.GameLoop.ReturnedToTitle += (sender, args) => cropLightManager.Reset();
            helper.Events.GameLoop.UpdateTicked += (sender, args) => journalBuilder?.UpdateQueuedRefresh();
            helper.Events.Input.ButtonsChanged += traitViewManager.OnButtonsChanged;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.Display.RenderedWorld += traitViewManager.OnRenderedWorld;
            helper.Events.Display.RenderedHud += traitViewManager.OnRenderedHud;
            helper.Events.GameLoop.UpdateTicked += mutationSparkleManager.OnUpdateTicked;
            helper.Events.GameLoop.UpdateTicked += cropLightManager.OnUpdateTicked;
            helper.Events.Player.InventoryChanged += (sender, args) => discoveryManager.RecordInventoryChanges(args);
            helper.Events.Multiplayer.ModMessageReceived += (sender, args) => discoveryManager.HandleModMessage(args);

            // Register commands
            helper.ConsoleCommands.Add("croptraits_set_traits", "croptraits_set_traits [Swift:1|Sweet:2]", SetTraits);
            helper.ConsoleCommands.Add("croptraits_list_traits", "croptraits_list_traits", ListTraits);
            helper.ConsoleCommands.Add("croptraits_discoveries", "croptraits_discoveries", ShowDiscoveries);
            helper.ConsoleCommands.Add("croptraits_open_journal", "croptraits_open_journal", OpenJournal);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            IParchmentApi parchmentApi = Helper.ModRegistry.GetApi<IParchmentApi>(PARCHMENT_ID);
            if (parchmentApi is null)
            {
                return;
            }

            journalBuilder = new JournalBuilder(parchmentApi);
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (journalBuilder is null || Context.IsPlayerFree is false || modConfig.OpenJournalKeybind.JustPressed() is false)
            {
                return;
            }

            Helper.Input.SuppressActiveKeybinds(modConfig.OpenJournalKeybind);
            journalBuilder.OpenBook();
        }

        public static void OpenJournal(string command, string[] args)
        {
            if (Context.IsWorldReady is false || journalBuilder is null)
            {
                return;
            }

            // Always force the book to rebuild when using the command
            if (journalBuilder.Build().TryOpen(out string error) is false)
            {
                monitor.Log($"Couldn't open the journal: {error}.", LogLevel.Warn);
            }
        }

        private void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
        {
            mutationSparkleManager.HandleAssetsInvalidated(e);

            // Other mods can add or change crops, so the produce to seed lookup is rebuilt
            if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo("Data/Crops")))
            {
                blendManager.InvalidateCropCache();
            }
        }

        public static void SetTraits(string command, string[] args)
        {
            if (Context.IsWorldReady is false)
            {
                monitor.Log("Load a save first.", LogLevel.Warn);
                return;
            }

            Item heldItem = Game1.player.CurrentItem;
            if (heldItem is null)
            {
                monitor.Log("Hold an item first.", LogLevel.Warn);
                return;
            }

            ArgUtility.TryGetOptional(args, 0, out string traitKey, out _, defaultValue: string.Empty);

            TraitSet traits = TraitSet.FromKey(traitKey);
            List<string> unknownIds = traits.Ids.Where(traitId => traitManager.TryGetTrait(traitId, out Trait _) is false).ToList();
            if (unknownIds.Count > 0)
            {
                monitor.Log($"Unknown traits: {string.Join(", ", unknownIds)}", LogLevel.Warn);
                return;
            }

            TraitModDataHelper.SetTraits(heldItem.modData, traits);

            // Produce needs its seed ID so the Seed Maker and Seed Splicer accept it
            if (traits.Count == 0)
            {
                heldItem.modData.Remove(SEED_ID_MOD_DATA_KEY);
            }
            else if (heldItem.Category != StardewValley.Object.SeedsCategory)
            {
                string seedId = blendManager.GetSeedIdByReverseLookup(heldItem);
                if (seedId is not null)
                {
                    heldItem.modData[SEED_ID_MOD_DATA_KEY] = seedId;
                }
            }

            // Changing an item already in the inventory fires no inventory event, so it's recorded here
            discoveryManager.RecordItem(heldItem);

            monitor.Log($"Held item is now {heldItem.DisplayName}.", LogLevel.Info);
        }

        public static void ListTraits(string command, string[] args)
        {
            foreach (Trait trait in traitManager.Traits.Values.OrderBy(trait => trait.Id, StringComparer.Ordinal))
            {
                monitor.Log($"{trait.Id}: max tier {trait.MaxTier}, weight {trait.Weight}, polarity {trait.Polarity}, price {trait.IsPriceTrait}, rollable {trait.CanBeRolled}", LogLevel.Info);
            }
        }

        public static void ShowDiscoveries(string command, string[] args)
        {
            DiscoveryData data = discoveryManager.Data;
            monitor.Log($"Traits: {data.Traits.Count}, crops: {data.Crops.Count}, combinations: {data.Combinations.Count}, journal entries: {data.BlendJournal.Count}", LogLevel.Info);

            foreach (KeyValuePair<string, TraitDiscoveryData> trait in data.Traits.OrderBy(trait => trait.Key, StringComparer.Ordinal))
            {
                monitor.Log($"  {trait.Key}: tiers {string.Join(", ", trait.Value.TiersSeen.OrderBy(tier => tier))}, first seen on {trait.Value.FirstSeedId ?? "unknown"}", LogLevel.Info);
            }
        }
    }
}
