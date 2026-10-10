using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;
using System.Linq;
using System.Reflection;

namespace CropTraits.Framework.Patches.Objects
{
    public class CaskPatch : PatchTemplate
    {
        private readonly System.Type _cask = typeof(Cask);

        public CaskPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            // Skipped with a warning if missing, so a changed game method only turns off traits like Aged instead of every patch after it
            MethodInfo dropInMethod = AccessTools.GetDeclaredMethods(_cask).FirstOrDefault(method => method.Name == nameof(Cask.performObjectDropInAction));
            if (dropInMethod is null)
            {
                _monitor.Log("Couldn't find Cask.performObjectDropInAction, so traits like Aged won't change how fast casks age.", LogLevel.Warn);
                return;
            }

            harmony.Patch(dropInMethod, prefix: new HarmonyMethod(GetType(), nameof(PerformObjectDropInActionPrefix)), postfix: new HarmonyMethod(GetType(), nameof(PerformObjectDropInActionPostfix)));
        }

        // The held item is captured first, so the postfix can tell a real drop-in (a new held item) from the cask checking what it accepts
        private static void PerformObjectDropInActionPrefix(Cask __instance, out StardewValley.Object __state)
        {
            __state = __instance.heldObject.Value;
        }

        // Traits like Aged scale the rate the game just set, reading the artisan good now in the cask
        private static void PerformObjectDropInActionPostfix(Cask __instance, bool __result, StardewValley.Object __state)
        {
            StardewValley.Object heldObject = __instance.heldObject.Value;
            if (__result is false || heldObject is null || ReferenceEquals(heldObject, __state) || TraitModDataHelper.HasTraits(heldObject.modData) is false)
            {
                return;
            }

            double multiplier = CropTraits.traitManager.GetCaskAgingMultiplier(TraitModDataHelper.GetTraits(heldObject.modData));
            if (multiplier != 1)
            {
                __instance.agingRate.Value = (float)(__instance.agingRate.Value * multiplier);
            }
        }
    }
}
