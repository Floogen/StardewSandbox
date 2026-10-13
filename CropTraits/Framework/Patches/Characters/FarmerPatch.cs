using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System.Linq;
using System.Reflection;

namespace CropTraits.Framework.Patches.Characters
{
    public class FarmerPatch : PatchTemplate
    {
        private readonly System.Type _farmer = typeof(Farmer);

        public FarmerPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            // Skipped with a warning if missing, so a changed game method only turns off Gilded's hand-picked harvests instead of every patch after it
            MethodInfo addItemMethod = AccessTools.GetDeclaredMethods(_farmer).FirstOrDefault(method => method.Name == nameof(Farmer.addItemToInventoryBool) && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(Item));
            if (addItemMethod is null)
            {
                _monitor.Log("Couldn't find Farmer.addItemToInventoryBool, so traits like Gilded won't change hand-picked harvests.", LogLevel.Warn);
                return;
            }

            harmony.Patch(addItemMethod, prefix: new HarmonyMethod(GetType(), nameof(AddItemToInventoryBoolPrefix)));
        }

        // A hand-picked harvest reaches the player here, so traits like Gilded can take it. Reporting it as added lets the harvest finish even with a full inventory
        private static bool AddItemToInventoryBoolPrefix(Item item, ref bool __result)
        {
            if (CropTraits.harvestManager.TryReplaceMainHarvest(item) is false)
            {
                return true;
            }

            __result = true;
            return false;
        }
    }
}
