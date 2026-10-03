using CropTraits.Framework.Models;
using StardewValley;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class ProduceScaleHelper
    {
        public static float GetScale(Item item)
        {
            if (item is null || item.Category == Object.SeedsCategory || item.modData.ContainsKey(CropTraits.SEED_ID_MOD_DATA_KEY) is false)
            {
                return 1f;
            }

            TraitSet traits = TraitModDataHelper.GetTraits(item.modData);
            return traits.Count == 0 ? 1f : CropTraits.traitManager.GetProduceScale(traits);
        }
    }
}
