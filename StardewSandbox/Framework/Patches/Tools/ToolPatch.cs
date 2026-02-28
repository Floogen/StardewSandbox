using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewSandbox.Framework.Objects;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.Text;

namespace StardewSandbox.Framework.Patches.Tools
{
    internal class ToolPatch : PatchTemplate
    {
        private readonly Type _object = typeof(Tool);

        public ToolPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(Tool.attachmentSlots)), postfix: new HarmonyMethod(GetType(), nameof(AttachmentSlotsPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Tool.canThisBeAttached), new[] { typeof(StardewValley.Object), typeof(int) }), postfix: new HarmonyMethod(GetType(), nameof(CanThisBeAttachedPostfix)));

            harmony.Patch(AccessTools.Method(_object, nameof(Tool.drawInMenu), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }), postfix: new HarmonyMethod(GetType(), nameof(DrawInMenuPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Tool.drawInMenu), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }), prefix: new HarmonyMethod(GetType(), nameof(DrawInMenuPrefix)));
            
            harmony.Patch(AccessTools.Method(_object, nameof(Tool.drawTooltip)), postfix: new HarmonyMethod(GetType(), nameof(DrawTooltipPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Tool.beginUsing), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }), prefix: new HarmonyMethod(GetType(), nameof(BeginUsingPrefix)));

            harmony.Patch(AccessTools.Method(_object, nameof(Tool.tickUpdate), new[] { typeof(GameTime), typeof(Farmer) }), postfix: new HarmonyMethod(GetType(), nameof(TickUpdatePostfix)));
        }

        private static void AttachmentSlotsPostfix(Tool __instance, ref int __result)
        {
            if (PocketWatch.IsToolPocketWatch(__instance))
            {
                __result = 1;
            }
        }

        private static void CanThisBeAttachedPostfix(Tool __instance, ref bool __result, StardewValley.Object o, int slot)
        {
            if (PocketWatch.IsToolPocketWatch(__instance))
            {
                __result = PocketWatch.IsFuel(o);
            }
        }

        private static bool DrawInMenuPrefix(Tool __instance, SpriteBatch spriteBatch, Vector2 location, ref float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow, out float __state)
        {
            __state = scaleSize;

            if (PocketWatch.IsToolPocketWatch(__instance) && ModEntry.toolManager.GetPocketWatch(__instance) is PocketWatch pocketWatch && pocketWatch is not null)
            {
                scaleSize = pocketWatch.GetScaleSize();
            }

            return true;
        }

        private static void DrawInMenuPostfix(Tool __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow, float __state)
        {
            if (PocketWatch.IsToolPocketWatch(__instance) && ModEntry.toolManager.GetPocketWatch(__instance) is PocketWatch pocketWatch && pocketWatch is not null)
            {
                var originalScaleSize = __state;

                var charges = pocketWatch.Charges;
                if (charges > 0)
                {
                    Utility.drawTinyDigits(charges, spriteBatch, location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(charges, 3f * originalScaleSize)) + 3f * originalScaleSize, 64f - 18f * originalScaleSize + 2f), 3f * originalScaleSize, 1f, Color.White);
                }

                var secondsRemainingForCharge = pocketWatch.SecondsRemainingForCharge;
                if (secondsRemainingForCharge > 0)
                {
                    Utility.drawTinyDigits(secondsRemainingForCharge, spriteBatch, location + new Vector2(4f, 4f), 3f * originalScaleSize, 1f, Color.White);
                }
            }
        }

        private static void DrawTooltipPostfix(Tool __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
        {
        }

        private static bool BeginUsingPrefix(Tool __instance, ref bool __result, GameLocation location, int x, int y, Farmer who)
        {
            if (ModEntry.toolManager.GetPocketWatch(__instance) is PocketWatch pocketWatch && pocketWatch is not null)
            {
                if (pocketWatch.IsActive)
                {
                    pocketWatch.Deactivate();
                }
                else
                {
                    pocketWatch.Activate();
                }
                __result = true;

                who.CanMove = true;
                who.UsingTool = false;
                return false;
            }

            return true;
        }

        private static void TickUpdatePostfix(Tool __instance, GameTime time, Farmer who)
        {
            if (PocketWatch.IsToolPocketWatch(__instance) && ModEntry.toolManager.GetPocketWatch(__instance) is PocketWatch pocketWatch && pocketWatch is not null)
            {
                pocketWatch.Update(time);
            }
        }
    }
}