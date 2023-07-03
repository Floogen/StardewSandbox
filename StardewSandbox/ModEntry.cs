﻿using HarmonyLib;
using StardewModdingAPI;
using StardewSandbox.Framework.Patches.Locations;
using StardewSandbox.Framework.Patches.xTiles;
using StardewValley;
using System;

namespace StardewSandbox
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;

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
                new GameLocationPatch(monitor, modHelper).Apply(harmony);
                new ForestPatch(monitor, modHelper).Apply(harmony);
                new LayerPatch(monitor, modHelper).Apply(harmony);
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Hook into the required events
        }
    }
}
