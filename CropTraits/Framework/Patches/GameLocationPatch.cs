using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CropTraits.Framework.Patches
{
    public class GameLocationPatch : PatchTemplate
    {
        private readonly System.Type _gameLocation = typeof(GameLocation);

        public GameLocationPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_gameLocation, nameof(GameLocation.SeedsIgnoreSeasonsHere), null), postfix: new HarmonyMethod(GetType(), nameof(SeedsIgnoreSeasonsHerePostfix)));
        }

        // Only true while a seed that grows in every season is being checked or planted (see PlantingHelper)
        private static void SeedsIgnoreSeasonsHerePostfix(ref bool __result)
        {
            if (__result is false && PlantingHelper.IsPlantingSeasonlessSeed)
            {
                __result = true;
            }
        }
    }
}
