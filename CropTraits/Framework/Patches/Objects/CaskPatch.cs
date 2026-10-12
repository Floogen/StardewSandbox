using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
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
            MethodInfo outputCaskMethod = AccessTools.Method(_cask, nameof(Cask.OutputCask));
            if (outputCaskMethod is null)
            {
                _monitor.Log("Couldn't find Cask.OutputCask, so traits like Aged won't change how fast casks age.", LogLevel.Warn);
                return;
            }

            harmony.Patch(outputCaskMethod, postfix: new HarmonyMethod(GetType(), nameof(OutputCaskPostfix)));
        }

        private static void OutputCaskPostfix(StardewValley.Object machine, bool probe, Item __result)
        {
            if (probe || machine is not Cask cask || __result is null || TraitModDataHelper.HasTraits(__result.modData) is false)
            {
                return;
            }

            // The output is a copy of the artisan good going into the cask, so it carries the same traits
            double multiplier = CropTraits.traitManager.GetCaskAgingMultiplier(TraitModDataHelper.GetTraits(__result.modData));
            if (multiplier != 1)
            {
                cask.agingRate.Value = (float)(cask.agingRate.Value * multiplier);
            }
        }
    }
}
