using HarmonyLib;
using StardewModdingAPI;
using StardewSandbox.Framework.Interfaces;
using StardewSandbox.Framework.Patches.Entities;
using StardewSandbox.Framework.Patches.Locations;
using StardewSandbox.Framework.Patches.xTiles;
using StardewValley;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace StardewSandbox
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static IManifest manifest;
        internal static Multiplayer multiplayer;

        // APIs
        internal static IFashionSenseApi fashionSenseApi;

        private static Queue<string> _queuedMessages = new Queue<string>();

        private const string SPECIAL_PROJECT_KEY = "PeacefulEnd.SpecialProjects.Active";
        private const string FASHIONABLE_HAT_KEY = "PeacefulEnd.MouseHouse.FashionableHats";
        private const string FASHION_SENSE_PACK_ID = "PeacefulEnd.AMouseWithAHat.FS";

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            manifest = ModManifest;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply patches
                new GameLocationPatch(monitor, modHelper).Apply(harmony);
                new ForestPatch(monitor, modHelper).Apply(harmony);
                new TownPatch(monitor, modHelper).Apply(harmony);

                new LayerPatch(monitor, modHelper).Apply(harmony);

                new NPCPatch(monitor, modHelper).Apply(harmony);
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Hook into the required events
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnDayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            ModEntry.SetActiveSpecialProjectId("RepairHatShop");
            if (fashionSenseApi is null)
            {
                return;
            }

            foreach (var hatName in GetUnlockedHats(Game1.player))
            {
                // Set unlocked via Fashion Sense's API
                fashionSenseApi.SetAppearanceLockState(IFashionSenseApi.Type.Hat, FASHION_SENSE_PACK_ID, hatName, false, manifest);
            }
        }

        private void OnGameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            // Hook into the APIs we utilize
            if (Helper.ModRegistry.IsLoaded("PeacefulEnd.FashionSense"))
            {
                fashionSenseApi = modHelper.ModRegistry.GetApi<IFashionSenseApi>("PeacefulEnd.FashionSense");

                if (fashionSenseApi is null)
                {
                    monitor.Log("Failed to hook into PeacefulEnd.FashionSense.", LogLevel.Error);
                }
            }
        }

        private void OnUpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (Game1.activeClickableMenu is not null)
            {
                return;
            }

            if (_queuedMessages.Count > 0 && _queuedMessages.TryDequeue(out var message))
            {
                // Display unlock message
                Game1.addHUDMessage(new HUDMessage(message, null));
            }
        }

        internal static HashSet<string> GetUnlockedHats(Farmer who)
        {
            HashSet<string> unlockedHatIds = new HashSet<string>();
            if (who.modData.ContainsKey(FASHIONABLE_HAT_KEY))
            {
                unlockedHatIds = JsonSerializer.Deserialize<HashSet<string>>(who.modData[FASHIONABLE_HAT_KEY]);
            }

            return unlockedHatIds;
        }

        internal static void UnlockFashionableHat(Farmer who, string hatName)
        {
            if (who is null || fashionSenseApi is null)
            {
                return;
            }

            HashSet<string> unlockedHatIds = GetUnlockedHats(who);

            // Add the unlocked hat
            if (unlockedHatIds.Add(hatName))
            {
                // Queue the message
                _queuedMessages.Enqueue($"[Hand Mirror]\nUnlocked the {hatName} hat.");

                // Set unlocked via Fashion Sense's API
                fashionSenseApi.SetAppearanceLockState(IFashionSenseApi.Type.Hat, FASHION_SENSE_PACK_ID, hatName, false, manifest);
            }

            // Preserve the changes
            who.modData[FASHIONABLE_HAT_KEY] = JsonSerializer.Serialize(unlockedHatIds);
        }

        internal static string GetActiveSpecialProjectId()
        {
            if (Game1.MasterPlayer.modData.ContainsKey(SPECIAL_PROJECT_KEY))
            {
                return Game1.MasterPlayer.modData[SPECIAL_PROJECT_KEY];
            }

            return string.Empty;
        }

        internal static void SetActiveSpecialProjectId(string project)
        {
            Game1.MasterPlayer.modData[SPECIAL_PROJECT_KEY] = project;
        }
    }
}
