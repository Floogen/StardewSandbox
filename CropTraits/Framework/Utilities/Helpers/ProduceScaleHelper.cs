using CropTraits.Framework.Models;
using StardewValley;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class ProduceScaleHelper
    {
        private static Item _menuDrawItem;
        private static float _menuDrawSlotScale;

        public static float GetScale(Item item)
        {
            if (item is null || item.Category == Object.SeedsCategory || item.modData.ContainsKey(CropTraits.SEED_ID_MOD_DATA_KEY) is false)
            {
                return 1f;
            }

            TraitSet traits = TraitModDataHelper.GetTraits(item.modData);
            return traits.Count == 0 ? 1f : CropTraits.traitManager.GetProduceScale(traits);
        }

        public static void BeginMenuDraw(Item item, float slotScaleSize)
        {
            _menuDrawItem = item;
            _menuDrawSlotScale = slotScaleSize;
        }

        public static void EndMenuDraw(Item item)
        {
            if (_menuDrawItem == item)
            {
                _menuDrawItem = null;
            }
        }

        public static bool TryGetSlotScale(Item item, out float slotScaleSize)
        {
            slotScaleSize = _menuDrawSlotScale;
            return item is not null && _menuDrawItem == item;
        }
    }
}
