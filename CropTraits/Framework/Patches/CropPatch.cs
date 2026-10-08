using CropTraits.Framework.Models;
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
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.newDay), null), prefix: new HarmonyMethod(GetType(), nameof(NewDayPrefix)), postfix: new HarmonyMethod(GetType(), nameof(NewDayPostfix)));
            harmony.Patch(AccessTools.Method(_crop, nameof(Crop.IsInSeason), new[] { typeof(GameLocation) }), postfix: new HarmonyMethod(GetType(), nameof(IsInSeasonPostfix)));
        }

        // The state argument is changed directly, so a crop can grow as if watered without changing how its soil looks
        private static void NewDayPrefix(Crop __instance, ref int state, out CropNightState __state)
        {
            __state = CropTraits.nightlyGrowthManager.BeginCropNight(__instance, ref state);
        }

        private static void NewDayPostfix(Crop __instance, CropNightState __state)
        {
            CropTraits.nightlyGrowthManager.EndCropNight(__instance, __state);
        }

        // Covers every season check the game makes for outdoor crops (the kill in newDay, the winter removal and fertilizer clearing)
        private static void IsInSeasonPostfix(Crop __instance, GameLocation location, ref bool __result)
        {
            if (__result is false && CropTraits.nightlyGrowthManager.ExtendsSeason(__instance, location))
            {
                __result = true;
            }
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
