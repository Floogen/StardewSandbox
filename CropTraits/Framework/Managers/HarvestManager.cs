using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Crops;
using StardewValley.TerrainFeatures;
using System;

namespace CropTraits.Framework.Managers
{
    public class HarvestManager : BaseManager
    {
        public const string REGROWTHS_USED_MOD_DATA_KEY = "PeacefulEnd.CropTraits/RegrowthsUsed"; // Regrowths a single harvest crop has used from traits like Resprouting, on HoeDirt
        public const int QUALITY_RANDOM_SEED = 4243; // Keeps the quality roll apart from other rolls made on the same tile and day

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

        // Called from Crop.harvest right after the game picks the quality, so the change carries into every copy the harvest makes
        public int ModifyHarvestQuality(int quality, Crop crop)
        {
            HarvestContext context = _activeHarvest;
            if (context is null || crop is null)
            {
                return quality;
            }

            Random random = Utility.CreateDaySaveRandom(crop.tilePosition.X, crop.tilePosition.Y, QUALITY_RANDOM_SEED);

            int modifiedQuality = quality;
            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(context.Traits))
            {
                modifiedQuality = trait.ModifyHarvestQuality(modifiedQuality, random, tier);
            }

            if (modifiedQuality == quality)
            {
                return quality;
            }

            // Matches the game's own clamp to the crop's quality limits
            CropData cropData = crop.GetData();
            return Math.Clamp(modifiedQuality, cropData?.HarvestMinQuality ?? StardewValley.Object.lowQuality, cropData?.HarvestMaxQuality ?? StardewValley.Object.bestQuality);
        }

        // Called from the Crop.harvest postfix, keeping a single harvest crop planted while its traits still allow a regrowth
        public void TryRegrow(Crop crop, HoeDirt soil, ref bool isCropRemoved)
        {
            HarvestContext context = _activeHarvest;
            if (isCropRemoved is false || context is null || context.CreatedItemCount == 0 || soil is null || soil.crop != crop)
            {
                return;
            }

            if (crop.dead.Value || crop.forageCrop.Value || crop.RegrowsAfterHarvest())
            {
                return;
            }

            int usedRegrowths = GetRegrowthsUsed(soil);
            if (usedRegrowths >= CropTraits.traitManager.GetExtraRegrowths(context.Traits))
            {
                return;
            }

            soil.modData[REGROWTHS_USED_MOD_DATA_KEY] = (usedRegrowths + 1).ToString();

            // Traits already seen at harvest stay visible while it regrows, so only new mutations are hidden until it's ready again
            soil.modData[CropTraits.SEED_TRAITS_MOD_DATA_KEY] = context.TraitKey;

            GrowthHelper.ResetToHalfwayGrowth(crop);
            isCropRemoved = false;
        }

        public static int GetRegrowthsUsed(HoeDirt soil)
        {
            if (soil.modData.TryGetValue(REGROWTHS_USED_MOD_DATA_KEY, out string value) && int.TryParse(value, out int regrowths))
            {
                return regrowths;
            }

            return 0;
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
