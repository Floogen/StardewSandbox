using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CropTraits.Framework.Patches
{
    public class UtilityPatch : PatchTemplate
    {
        private readonly System.Type _utility = typeof(Utility);

        public UtilityPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_utility, nameof(Utility.performLightningUpdate), null), prefix: new HarmonyMethod(GetType(), nameof(PerformLightningUpdatePrefix)), postfix: new HarmonyMethod(GetType(), nameof(PerformLightningUpdatePostfix)), finalizer: new HarmonyMethod(GetType(), nameof(PerformLightningUpdateFinalizer)));
        }

        // Covers both daytime storms and the overnight lightning the game runs after the player sleeps
        private static void PerformLightningUpdatePrefix()
        {
            LightningHelper.IsLightningUpdateActive = true;
        }

        // The time of day is read by position (__0), so a renamed game parameter can't break the patch
        private static void PerformLightningUpdatePostfix(int __0)
        {
            LightningHelper.IsLightningUpdateActive = false;
            LightningHelper.TryStrikeConductiveCrop(__0);
        }

        // Finalizer ensures the flag clears even if the update throws
        private static void PerformLightningUpdateFinalizer()
        {
            LightningHelper.IsLightningUpdateActive = false;
        }
    }
}
