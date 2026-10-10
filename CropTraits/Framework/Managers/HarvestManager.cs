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

            GameLocation location = soil.Location ?? crop.currentLocation;
            _activeHarvest = new HarvestContext
            {
                TraitKey = traitKey,
                Traits = TraitSet.FromKey(traitKey),
                SeedId = crop.netSeedIndex.Value,
                QualifiedHarvestItemId = qualifiedHarvestItemId,
                Soil = soil,
                JunimoHarvester = junimoHarvester,
                IsOutOfSeason = location is not null && location.IsOutdoors && NightlyGrowthHelper.IsInSeasonIgnoringTraits(crop, location) is false
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

        public bool IsHarvestActive => _activeHarvest is not null;

        /// <summary>
        /// Called as an item reaches the player during a harvest (added to the inventory or dropped by a scythe), so traits like Gilded can take the main harvest instead.
        /// Only the first item matching the harvest is offered. Junimo harvests are left alone since there's no player to pay.
        /// </summary>
        public bool TryReplaceMainHarvest(Item item)
        {
            HarvestContext context = _activeHarvest;
            if (context is null || context.IsMainHarvestHandled || context.JunimoHarvester is not null || item is null || item.QualifiedItemId != context.QualifiedHarvestItemId)
            {
                return false;
            }

            context.IsMainHarvestHandled = true;
            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(context.Traits))
            {
                if (trait.TryReplaceMainHarvest(context, item, tier))
                {
                    return true;
                }
            }

            return false;
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
                modifiedQuality = trait.ModifyHarvestQuality(modifiedQuality, context, random, tier);
            }

            // Traits like Fragile drop quality for each full day the crop sat ready, while traits like Mellow raise it
            if (context.Soil is not null && CropTraits.traitManager.TracksDaysLeftReady(context.Traits))
            {
                modifiedQuality = QualityHelper.ShiftQuality(modifiedQuality, CropTraits.traitManager.GetLeftReadyQualityShift(context.Traits, NightlyGrowthHelper.GetDaysLeftReady(context.Soil)));
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

        // Called from the Crop.harvest postfix after TryRegrow, removing a crop that normally regrows when a trait like Spent says so
        public void TryRemoveSpentCrop(Crop crop, HoeDirt soil, ref bool isCropRemoved)
        {
            HarvestContext context = _activeHarvest;
            if (isCropRemoved || context is null || context.CreatedItemCount == 0 || soil is null || soil.crop != crop)
            {
                return;
            }

            if (crop.dead.Value || crop.RegrowsAfterHarvest() is false || CropTraits.traitManager.IsRemovedAfterHarvest(context.Traits) is false)
            {
                return;
            }

            isCropRemoved = true;
        }

        // Called from the Crop.harvest postfix after TryRegrow and TryRemoveSpentCrop, once it's settled that the crop leaves its soil
        public void CompleteCropRemoval(Crop crop, HoeDirt soil, bool isCropRemoved)
        {
            HarvestContext context = _activeHarvest;
            if (isCropRemoved is false || context is null || context.CreatedItemCount == 0 || soil is null || soil.crop != crop)
            {
                return;
            }

            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(context.Traits))
            {
                trait.OnCropRemovedAfterHarvest(context, tier);
            }
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
            if (context is null)
            {
                return;
            }

            // Anything created from here on is extra produce, which traits like Gilded never take
            context.IsMainHarvestHandled = true;

            if (context.CreatedItemCount == 0 || location is null)
            {
                return;
            }

            // A regrowing crop starts counting its ready days again from zero
            context.Soil?.modData.Remove(NightlyGrowthManager.DAYS_LEFT_READY_MOD_DATA_KEY);

            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(context.Traits))
            {
                trait.OnHarvestCompleted(context, xTile, yTile, location, tier);
            }
        }
    }
}
