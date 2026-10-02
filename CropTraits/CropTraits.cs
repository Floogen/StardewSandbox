using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Patches;
using CropTraits.Framework.Patches.Objects;
using CropTraits.Framework.Patches.TerrainFeatures;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
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

        // ModData keys
        public const string TRAITS_MOD_DATA_KEY = "PeacefulEnd.CropTraits/Traits"; // Trait key on seeds, produce, artisan goods and HoeDirt
        public const string SEED_TRAITS_MOD_DATA_KEY = "PeacefulEnd.CropTraits/SeedTraits"; // Seed's original traits on HoeDirt, shown until the crop reaches maturity
        public const string SEED_ID_MOD_DATA_KEY = "PeacefulEnd.CropTraits/SeedId"; // Seed ID that mutated produce was grown from
        public const string PLANTER_ID_MOD_DATA_KEY = "PeacefulEnd.CropTraits/PlanterId"; // Farmer who planted the crop on HoeDirt, so growth rebuilds use their professions

        // Shared static helpers
        public static IManifest manifest;
        public static IMonitor monitor;
        public static IModHelper modHelper;
        public static Multiplayer multiplayer;

        // Managers
        public static AssetManager assetManager;
        public static BlendManager blendManager;
        public static DiscoveryManager discoveryManager;
        public static HarvestManager harvestManager;
        public static MutationManager mutationManager;
        public static TraitManager traitManager;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            manifest = ModManifest;
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Create managers
            assetManager = new AssetManager(monitor, helper);
            blendManager = new BlendManager(monitor, helper);
            discoveryManager = new DiscoveryManager(monitor, helper);
            harvestManager = new HarvestManager(monitor, helper);
            mutationManager = new MutationManager(monitor, helper);
            traitManager = new TraitManager(monitor, helper);

            try
            {
                var harmony = new Harmony(ModManifest.UniqueID);

                // Apply patches
                new HoeDirtPatch(monitor, modHelper).Apply(harmony);
                new CropPatch(monitor, modHelper).Apply(harmony);
                new ItemRegistryPatch(monitor, modHelper).Apply(harmony);
                new ColoredObjectPatch(monitor, modHelper).Apply(harmony);
                new ObjectPatch(monitor, modHelper).Apply(harmony);
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
            helper.Events.GameLoop.DayEnding += (sender, args) => mutationManager.ResolveAllPendingMutations();
            helper.Events.GameLoop.SaveLoaded += (sender, args) => discoveryManager.Load();
            helper.Events.GameLoop.Saving += (sender, args) => discoveryManager.Save();
            helper.Events.GameLoop.ReturnedToTitle += (sender, args) => discoveryManager.Reset();
            helper.Events.Player.InventoryChanged += (sender, args) => discoveryManager.RecordInventoryChanges(args);
            helper.Events.Multiplayer.ModMessageReceived += (sender, args) => discoveryManager.HandleModMessage(args);

            // Register commands
            helper.ConsoleCommands.Add("croptraits_set_traits", "croptraits_set_traits [Swift:1|Sweet:2]", SetTraits);
            helper.ConsoleCommands.Add("croptraits_list_traits", "croptraits_list_traits", ListTraits);
            helper.ConsoleCommands.Add("croptraits_discoveries", "croptraits_discoveries", ShowDiscoveries);
        }

        private void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
        {
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

            ArgUtility.TryGetOptional(args, 0, out string traitKey, out string error, defaultValue: string.Empty);

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

            monitor.Log($"Held item is now {heldItem.DisplayName}.", LogLevel.Info);
        }

        public static void ListTraits(string command, string[] args)
        {
            foreach (Trait trait in traitManager.Traits.Values.OrderBy(trait => trait.Id, StringComparer.Ordinal))
            {
                monitor.Log($"{trait.Id}: max tier {trait.MaxTier}, weight {trait.Weight}, negative {trait.IsNegative}, price {trait.IsPriceTrait}, rollable {trait.CanBeRolled}", LogLevel.Info);
            }
        }

        public static void ShowDiscoveries(string command, string[] args)
        {
            DiscoveryData data = discoveryManager.Data;
            monitor.Log($"Traits: {data.Traits.Count}, combinations: {data.Combinations.Count}, journal entries: {data.BlendJournal.Count}", LogLevel.Info);

            foreach (KeyValuePair<string, TraitDiscoveryData> trait in data.Traits.OrderBy(trait => trait.Key, StringComparer.Ordinal))
            {
                monitor.Log($"  {trait.Key}: tiers {string.Join(", ", trait.Value.TiersSeen.OrderBy(tier => tier))}, first seen on {trait.Value.FirstSeedId ?? "unknown"}", LogLevel.Info);
            }
        }
    }
}
