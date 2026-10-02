using HarmonyLib;
using StardewModdingAPI;

namespace CropTraits.Framework.Patches
{
    public abstract class PatchTemplate
    {
        public static IMonitor _monitor;
        public static IModHelper _helper;

        public PatchTemplate(IMonitor modMonitor, IModHelper modHelper)
        {
            _monitor = modMonitor;
            _helper = modHelper;
        }

        public abstract void Apply(Harmony harmony);
    }
}
