using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Managers
{
    public class HarvestManager : BaseManager
    {
        // Active only while Crop.harvest runs for a traited crop
        private HarvestContext _activeHarvest;

        /// <summary>Tracks the crop being harvested so the produce it creates picks up its traits.</summary>
        public HarvestManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public void BeginHarvest(Crop crop, HoeDirt soil, JunimoHarvester junimoHarvester)
        {
            _activeHarvest = null;

            if (crop is null || soil is null)
            {
                return;
            }

            string traitKey = TraitModDataHelper.GetTraitKey(soil.modData);
            if (string.IsNullOrEmpty(traitKey))
            {
                return;
            }

            string qualifiedHarvestItemId = ItemRegistry.QualifyItemId(crop.indexOfHarvest.Value);
            if (string.IsNullOrEmpty(qualifiedHarvestItemId))
            {
                return;
            }

            _activeHarvest = new HarvestContext
            {
                TraitKey = traitKey,
                Traits = TraitSet.FromKey(traitKey),
                SeedId = crop.netSeedIndex.Value,
                QualifiedHarvestItemId = qualifiedHarvestItemId,
                JunimoHarvester = junimoHarvester
            };
        }

        public void EndHarvest()
        {
            _activeHarvest = null;
        }

        public void ApplyToHarvestedItem(Item item)
        {
            HarvestContext context = _activeHarvest;
            if (context is null || item is null || item.QualifiedItemId != context.QualifiedHarvestItemId)
            {
                return;
            }

            // Guards against applying twice if an item passes through both creation paths
            if (item.modData.ContainsKey(CropTraits.TRAITS_MOD_DATA_KEY))
            {
                return;
            }

            item.modData[CropTraits.TRAITS_MOD_DATA_KEY] = context.TraitKey;
            item.modData[CropTraits.SEED_ID_MOD_DATA_KEY] = context.SeedId;
            context.CreatedItemCount++;

            // Quality is assigned after creation, but the reference is only read once the harvest completes
            context.HarvestedItem ??= item;
        }

        // Called while the harvest context is still active, so anything a trait creates here picks up traits too
        public void CompleteHarvest(int xTile, int yTile, GameLocation location)
        {
            HarvestContext context = _activeHarvest;
            if (context is null || context.CreatedItemCount == 0 || location is null)
            {
                return;
            }

            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(context.Traits))
            {
                trait.OnHarvestCompleted(context, xTile, yTile, location, tier);
            }
        }
    }
}
