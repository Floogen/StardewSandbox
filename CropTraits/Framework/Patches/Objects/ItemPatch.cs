using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.Reflection;

namespace CropTraits.Framework.Patches.Objects
{
    public class ItemPatch : PatchTemplate
    {
        private readonly System.Type _item = typeof(Item);

        public ItemPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_item, nameof(Item.canStackWith), new[] { typeof(ISalable) }), postfix: new HarmonyMethod(GetType(), nameof(CanStackWithPostfix)));
            harmony.Patch(GetImplementationForObjects(nameof(Item.getExtraSpaceNeededForTooltipSpecialIcons)), postfix: new HarmonyMethod(GetType(), nameof(GetExtraSpaceNeededForTooltipSpecialIconsPostfix)));
            harmony.Patch(GetImplementationForObjects(nameof(Item.drawTooltip)), postfix: new HarmonyMethod(GetType(), nameof(DrawTooltipPostfix)));
        }

        private MethodInfo GetImplementationForObjects(string methodName)
        {
            MethodInfo method = AccessTools.Method(typeof(StardewValley.Object), methodName);
            return AccessTools.DeclaredMethod(method.DeclaringType, methodName);
        }

        private static void GetExtraSpaceNeededForTooltipSpecialIconsPostfix(Item __instance, SpriteFont font, int minWidth, int startingHeight, ref Point __result)
        {
            int traitHeight = TraitTooltipHelper.GetHeight(__instance, font);
            if (traitHeight == 0)
            {
                return;
            }

            int width = __result.X != 0 ? __result.X : minWidth;
            int height = __result.Y != 0 ? __result.Y : startingHeight;
            __result = new Point(width, height + traitHeight);
        }

        private static void DrawTooltipPostfix(Item __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha)
        {
            TraitTooltipHelper.DrawTraits(__instance, spriteBatch, x, ref y, font, alpha);
        }

        // Vanilla ignores modData, so items with different trait keys or seed IDs must stay apart
        private static void CanStackWithPostfix(Item __instance, ISalable other, ref bool __result)
        {
            if (__result is false || other is not Item otherItem)
            {
                return;
            }

            if (HasMatchingKey(__instance, otherItem, CropTraits.TRAITS_MOD_DATA_KEY) is false || HasMatchingKey(__instance, otherItem, CropTraits.SEED_ID_MOD_DATA_KEY) is false)
            {
                __result = false;
            }
        }

        // Two items both missing the key compare as equal, so plain items stack as in vanilla
        private static bool HasMatchingKey(Item firstItem, Item secondItem, string key)
        {
            firstItem.modData.TryGetValue(key, out string firstValue);
            secondItem.modData.TryGetValue(key, out string secondValue);

            return string.IsNullOrEmpty(firstValue) ? string.IsNullOrEmpty(secondValue) : firstValue == secondValue;
        }
    }
}
