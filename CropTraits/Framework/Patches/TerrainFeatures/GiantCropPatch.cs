using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Patches.TerrainFeatures
{
    public class GiantCropPatch : PatchTemplate
    {
        private readonly System.Type _giantCrop = typeof(GiantCrop);

        public GiantCropPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_giantCrop, "TryGetDrop"), postfix: new HarmonyMethod(GetType(), nameof(TryGetDropPostfix)));
        }

        private static void TryGetDropPostfix(GiantCrop __instance, bool isShaving, Item __result)
        {
            GiantCropHelper.ApplyToDrop(__instance, __result, isShaving);
        }
    }
}
