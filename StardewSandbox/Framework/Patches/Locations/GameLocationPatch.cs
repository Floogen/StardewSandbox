using HarmonyLib;
using StardewModdingAPI;
using HatShopRestoration.Framework.Patches;
using HatShopRestoration.Framework.UI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using xTile.Dimensions;
using xTile.Tiles;

namespace HatShopRestoration.Framework.Patches.Locations
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

        private static List<Response> GetSpecialProjects()
        {
            List<Response> options = new List<Response>();
            if (Game1.MasterPlayer.mailReceived.Contains("hatter") is true && Game1.MasterPlayer.mailReceived.Contains("HatShopRepaired") is false)
            {
                options.Add(new Response("RepairHatShop", "Repair Hat Shop"));
                options.Add(new Response("Exit", "Leave"));
            }

            return options;
        }

        private static List<Response> GetMouseOptions()
        {
            List<Response> options = new List<Response>()
            {
                new Response("mouseShop", "Purchase a hat"),
                new Response("mouseFashionSense", "Unlock hat customization"),
                new Response("exit", "Leave")
            };

            return options;
        }

        private static void HandleCarpenterOptions(List<Response> options)
        {
            if (GetSpecialProjects().Count > 0)
            {
                options.Add(new Response("SpecialProjects", "Special Projects"));
            }
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
                    Game1.drawObjectDialogue("You are a tad too tall to fit through this small door. It seems to be a tiny bedroom of sorts.");
                    break;
                case "MouseDialogue":
                    __result = true;
                    __instance.createQuestionDialogue("Hiyo, poke! Come for hats?", GetMouseOptions().ToArray(), "mouseDialogue");
                    break;
                default:
                    break;
            }
        }

        private static void AnswerDialogueActionPostfix(GameLocation __instance, ref bool __result, string questionAndAnswer, string[] questionParams)
        {
            if (questionAndAnswer == "carpenter_SpecialProjects")
            {
                // This line currently won't show, as we are preventing "Special Projects" dialogue option from showing when there are none available
                if (GetSpecialProjects().Count == 0)
                {
                    Game1.getCharacterFromName("Robin").setNewDialogue("$sThere are no projects available right now.");
                    Game1.drawDialogue(Game1.getCharacterFromName("Robin"));
                }
                else
                {
                    __instance.createQuestionDialogue("What project would you like to start?", GetSpecialProjects().ToArray(), "carpenter");
                }

                __result = true;
            }
            else if (questionAndAnswer == "carpenter_RepairHatShop")
            {
                __instance.createQuestionDialogue("You would like to convert that old abandoned house into a hat shop? It will cost 10,000g and you'll also need to provide me with 250 pieces of wood and 10 cloths.", __instance.createYesNoResponses(), "carpenter_RepairHatShop_Answer");
            }
            else if (questionAndAnswer == "carpenter_RepairHatShop_Answer_Yes")
            {
                if (Game1.player.Money >= 10000 && Game1.player.hasItemInInventory(388, 250) && Game1.player.hasItemInInventory(428, 10))
                {
                    ModEntry.SetActiveSpecialProjectId("RepairHatShop");

                    (Game1.getLocationFromName("Town") as Town).daysUntilCommunityUpgrade.Value = 3;
                    Game1.player.Money -= 10000;
                    Game1.player.removeItemsFromInventory(388, 250);
                    Game1.player.removeItemsFromInventory(428, 10);
                    Game1.getCharacterFromName("Robin").setNewDialogue(Game1.content.LoadString("Data\\ExtraDialogue:Robin_HouseUpgrade_Accepted"));
                    Game1.drawDialogue(Game1.getCharacterFromName("Robin"));
                    ModEntry.multiplayer.globalChatInfoMessage("RepairHatShop", Game1.player.Name, Lexicon.getPossessivePronoun(Game1.player.IsMale));
                }
                else if (Game1.player.Money < 10000)
                {
                    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney3"));
                }
                else if (Game1.player.hasItemInInventory(388, 250) is false)
                {
                    Game1.drawObjectDialogue("Sorry... You have the money, but I also need the 250 pieces of wood.");
                }
                else if (Game1.player.hasItemInInventory(428, 10) is false)
                {
                    Game1.drawObjectDialogue("Sorry... You have the money and wood, but I also need the 10 bolts of cloth.");
                }
            }
            else if (questionAndAnswer == "mouseDialogue_mouseShop")
            {
                __result = true;
                Game1.activeClickableMenu = new ShopMenu(Utility.getHatStock(), 0, "HatMouse");
            }
            else if (questionAndAnswer == "mouseDialogue_mouseFashionSense")
            {
                __result = true;
                Game1.activeClickableMenu = new MouseShopMenu(Utility.getHatStock());
            }
            else if (questionAndAnswer == "mouseDialogue_leave")
            {
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

                return list;
            }
            catch (Exception e)
            {
                _monitor.Log($"There was an issue modifying the instructions for StardewValley.GameLocation.carpenters: {e}", LogLevel.Error);
                return instructions;
            }
        }
    }
}