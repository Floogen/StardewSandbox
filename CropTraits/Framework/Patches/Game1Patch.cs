using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System.Linq;
using System.Reflection;

namespace CropTraits.Framework.Patches
{
    public class Game1Patch : PatchTemplate
    {
        private readonly System.Type _game1 = typeof(Game1);

        public Game1Patch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            // Skipped with a warning if missing, so a changed game method only turns off Gilded's scythe harvests instead of every patch after it
            MethodInfo createItemDebrisMethod = AccessTools.GetDeclaredMethods(_game1).FirstOrDefault(method => method.Name == nameof(Game1.createItemDebris) && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(Item));
            if (createItemDebrisMethod is null)
            {
                _monitor.Log("Couldn't find Game1.createItemDebris, so traits like Gilded won't change scythe harvests.", LogLevel.Warn);
                return;
            }

            harmony.Patch(createItemDebrisMethod, prefix: new HarmonyMethod(GetType(), nameof(CreateItemDebrisPrefix)));
        }

        // A scythe harvest drops its main harvest here instead of adding it to the inventory, so traits like Gilded can take it before it lands
        private static bool CreateItemDebrisPrefix(Item item, ref Debris __result)
        {
            if (CropTraits.harvestManager.TryReplaceMainHarvest(item) is false)
            {
                return true;
            }

            __result = null;
            return false;
        }
    }
}
