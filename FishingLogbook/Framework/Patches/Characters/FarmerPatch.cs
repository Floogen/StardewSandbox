using FishingLogbook;
using FishingLogbook.Framework.Builders;
using FishingLogbook.Framework.Models.Fish;
using FishingLogbook.Framework.Patches;
using FishingLogbook.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.Patches.Objects
{
    internal class FarmerPatch : PatchTemplate
    {
        private readonly System.Type _farmer = typeof(Farmer);

        public FarmerPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_farmer, nameof(Farmer.caughtFish), new[] { typeof(string), typeof(int), typeof(bool), typeof(int) }), postfix: new HarmonyMethod(GetType(), nameof(PerformUseActionPostfix)));
        }

        private static void PerformUseActionPostfix(Farmer __instance, ref bool __result, string itemId, int size, bool from_fish_pond, int numberCaught)
        {
            string qualifiedId = ItemRegistry.QualifyItemId(itemId);
            if (qualifiedId is null)
            {
                return;
            }

            RecentCatchHelper.AddRecentCatch(__instance, itemId, size);

            ModEntry.logbookBuilder.RefreshBook();
        }
    }
}
