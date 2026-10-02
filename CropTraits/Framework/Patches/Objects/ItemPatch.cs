using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

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
