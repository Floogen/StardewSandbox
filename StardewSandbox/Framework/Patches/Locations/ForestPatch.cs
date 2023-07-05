using HarmonyLib;
using StardewModdingAPI;
using StardewSandbox.Framework.Patches;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;

namespace StardewSandbox.Framework.Patches.Locations
{
    internal class ForestPatch : PatchTemplate
    {
        private readonly System.Type _object = typeof(Forest);

        public ForestPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(Forest.checkAction), new[] { typeof(xTile.Dimensions.Location), typeof(xTile.Dimensions.Rectangle), typeof(Farmer) }), prefix: new HarmonyMethod(GetType(), nameof(CheckActionPatchPrefix)));
        }

        private static bool CheckActionPatchPrefix(Forest __instance, ref bool __result, xTile.Dimensions.Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
        {
            int tileIndexOfCheckLocation = ((__instance.map.GetLayer("Buildings").Tiles[tileLocation] != null) ? __instance.map.GetLayer("Buildings").Tiles[tileLocation].TileIndex : (-1));
            if (tileIndexOfCheckLocation == 1972 && Game1.MasterPlayer.mailReceived.Contains("HatShopRepaired"))
            {
                __result = true;

                Game1.warpFarmer("Custom_PeacefulEnd_MouseShop", 3, 11, false);

                return false;
            }

            return true;
        }
    }
}