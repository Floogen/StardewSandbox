using HarmonyLib;
using StardewModdingAPI;
using StardewSandbox.Framework.Patches;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using xTile.Dimensions;
using xTile.Tiles;

namespace StardewSandbox.Framework.Patches.Locations
{
    internal class GameLocationPatch : PatchTemplate
    {
        private readonly System.Type _object = typeof(GameLocation);

        public GameLocationPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(GameLocation.checkAction), new[] { typeof(xTile.Dimensions.Location), typeof(xTile.Dimensions.Rectangle), typeof(Farmer) }), postfix: new HarmonyMethod(GetType(), nameof(CheckActionPatchPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(GameLocation.answerDialogueAction), new[] { typeof(string), typeof(string[]) }), postfix: new HarmonyMethod(GetType(), nameof(AnswerDialogueActionPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(GameLocation.carpenters), new[] { typeof(Location) }), transpiler: new HarmonyMethod(GetType(), nameof(CarpentersTranspiler)));
        }

        private static void CheckActionPatchPostfix(GameLocation __instance, ref bool __result, xTile.Dimensions.Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
        {
            if (__result)
            {
                return;
            }

            // Check for custom actions
            Tile tile = __instance.map.GetLayer("Buildings").PickTile(new xTile.Dimensions.Location(tileLocation.X * 64, tileLocation.Y * 64), viewport.Size);
            if (tile is null || !tile.Properties.ContainsKey("Action"))
            {
                return;
            }

            switch (tile.Properties["Action"].ToString())
            {
                case "MouseHome":
                    __result = true;
                    Game1.drawObjectDialogue("You are a tad too tall to fit inside this small door. From what you can see, there seems to be a tiny bedroom of sorts.");
                    break;
                case "MouseShop":
                    __result = true;
                    Game1.activeClickableMenu = new ShopMenu(Utility.getHatStock(), 0, "HatMouse");
                    break;
                case "MouseFashionSense":
                    __result = true;
                    var shopMenu = new ShopMenu(new Dictionary<ISalable, int[]>());
                    // TODO: Implement menu to purchase FS unlocks
                    Game1.activeClickableMenu = shopMenu;
                    break;
                default:
                    break;
            }
        }

        private static List<Response> GetSpecialProjects()
        {
            List<Response> options = new List<Response>();
            if (Game1.MasterPlayer.mailReceived.Contains("mouseHouseUpgrade") is false)
            {
                options.Add(new Response("mouseHouseUpgrade", "Repair Hat Shop"));
            }

            return options;
        }

        private static void AnswerDialogueActionPostfix(GameLocation __instance, ref bool __result, string questionAndAnswer, string[] questionParams)
        {
            if (questionAndAnswer == "carpenter_SpecialProjects")
            {
                if (GetSpecialProjects().Count == 0)
                {
                    Game1.getCharacterFromName("Robin").setNewDialogue("$sThere are no projects available right now.");
                    Game1.drawDialogue(Game1.getCharacterFromName("Robin"));
                }
                else
                {
                    __instance.createQuestionDialogue("What project would you like to start?", GetSpecialProjects().ToArray(), "specialProjectStart");
                }

                __result = true;
            }
        }

        private static IEnumerable<CodeInstruction> CarpentersTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var list = instructions.ToList();

                // Get the indices to insert at
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].opcode == OpCodes.Ldstr && list[i].operand.ToString().Contains("Construct", StringComparison.OrdinalIgnoreCase))
                    {
                        list.Insert(i, new CodeInstruction(OpCodes.Ldloc_3));
                        list.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameLocationPatch), nameof(HandleCarpenterOptions))));
                        break;
                    }
                }


                for (int i = 0; i < list.Count; i++)
                {
                    _monitor.Log($"{list[i].opcode} -> {list[i].operand}");
                }

                return list;
            }
            catch (Exception e)
            {
                _monitor.Log($"There was an issue modifying the instructions for StardewValley.GameLocation.carpenters: {e}", LogLevel.Error);
                return instructions;
            }
        }

        private static void HandleCarpenterOptions(List<Response> options)
        {
            options.Add(new Response("SpecialProjects", "Special Projects"));
        }
    }
}