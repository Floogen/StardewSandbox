using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Patches
{
    public class CropPatch : PatchTemplate
    {
        private readonly System.Type _crop = typeof(Crop);

        public CropPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.harvest), null), prefix: new HarmonyMethod(GetType(), nameof(HarvestPrefix)), postfix: new HarmonyMethod(GetType(), nameof(HarvestPostfix)), finalizer: new HarmonyMethod(GetType(), nameof(HarvestFinalizer)));
        }

        private static void HarvestPrefix(Crop __instance, HoeDirt soil, JunimoHarvester junimoHarvester)
        {
            CropTraits.harvestManager.BeginHarvest(__instance, soil, junimoHarvester);
        }

        private static void HarvestPostfix(int xTile, int yTile, HoeDirt soil)
        {
            CropTraits.harvestManager.CompleteHarvest(xTile, yTile, soil?.Location ?? Game1.currentLocation);
        }

        // Finalizer ensures the harvest context clears even if harvest throws
        private static void HarvestFinalizer()
        {
            CropTraits.harvestManager.EndHarvest();
        }
    }
}
