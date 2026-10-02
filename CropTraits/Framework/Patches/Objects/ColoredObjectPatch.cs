using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Objects;

namespace CropTraits.Framework.Patches.Objects
{
    public class ColoredObjectPatch : PatchTemplate
    {
        private readonly System.Type _coloredObject = typeof(ColoredObject);

        public ColoredObjectPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Constructor(_coloredObject, new[] { typeof(string), typeof(int), typeof(Color) }), postfix: new HarmonyMethod(GetType(), nameof(ColoredObjectPostfix)));
        }

        // Colored crops (flowers) are constructed directly rather than through ItemRegistry
        private static void ColoredObjectPostfix(ColoredObject __instance)
        {
            CropTraits.harvestManager.ApplyToHarvestedItem(__instance);
        }
    }
}
