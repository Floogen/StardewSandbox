using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
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
            harmony.Patch(AccessTools.Method(_coloredObject, nameof(ColoredObject.drawInMenu), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }), postfix: new HarmonyMethod(GetType(), nameof(DrawInMenuPostfix)));
        }

        private static void DrawInMenuPostfix(ColoredObject __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber)
        {
            MutationBadgeHelper.DrawBadge(__instance, spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber);
        }

        // Colored crops (flowers) are constructed directly rather than through ItemRegistry
        private static void ColoredObjectPostfix(ColoredObject __instance)
        {
            CropTraits.harvestManager.ApplyToHarvestedItem(__instance);
        }
    }
}
