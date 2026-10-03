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
            harmony.Patch(AccessTools.Method(_coloredObject, nameof(ColoredObject.drawInMenu), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }), prefix: new HarmonyMethod(GetType(), nameof(DrawInMenuPrefix)), postfix: new HarmonyMethod(GetType(), nameof(DrawInMenuPostfix)));
        }

        private static void DrawInMenuPrefix(ColoredObject __instance, ref float scaleSize, out float __state)
        {
            __state = scaleSize;

            float produceScale = ProduceScaleHelper.GetScale(__instance);
            if (produceScale == 1f)
            {
                return;
            }

            ProduceScaleHelper.BeginMenuDraw(__instance, scaleSize);
            scaleSize *= produceScale;
        }

        private static void DrawInMenuPostfix(ColoredObject __instance, SpriteBatch spriteBatch, Vector2 location, float transparency, float layerDepth, StackDrawType drawStackNumber, float __state)
        {
            ProduceScaleHelper.EndMenuDraw(__instance);
            MutationBadgeHelper.DrawBadge(__instance, spriteBatch, location, __state, transparency, layerDepth, drawStackNumber);
        }

        // Colored crops (flowers) are constructed directly rather than through ItemRegistry
        private static void ColoredObjectPostfix(ColoredObject __instance)
        {
            CropTraits.harvestManager.ApplyToHarvestedItem(__instance);
        }
    }
}
