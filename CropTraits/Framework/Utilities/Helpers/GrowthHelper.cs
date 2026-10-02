using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Utilities.Helpers
{
    // Applies growth traits by adjusting a crop's phase lengths
    public static class GrowthHelper
    {
        public const int FINAL_PHASE_LENGTH = 99999;
        public const int MAX_REMOVAL_PASSES = 3;

        // Applies growth traits on top of the crop's current phases (used right after planting)
        public static void ApplyGrowthTraits(Crop crop, TraitSet traits)
        {
            if (crop is null || traits is null || crop.phaseDays.Count < 2)
            {
                return;
            }

            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(traits))
            {
                trait.ModifyGrowth(crop, tier);
            }
        }

        // Rebuilds phases from crop data so growth traits never double up or get wiped by vanilla speed increases
        public static void ReapplyGrowthTraits(HoeDirt dirt, TraitSet traits, Farmer planter)
        {
            Crop crop = dirt.crop;
            CropData cropData = crop?.GetData();
            if (cropData?.DaysInPhase is null || cropData.DaysInPhase.Count == 0)
            {
                return;
            }

            crop.phaseDays.Clear();
            foreach (int days in cropData.DaysInPhase)
            {
                crop.phaseDays.Add(days);
            }

            crop.phaseDays.Add(FINAL_PHASE_LENGTH);

            // Vanilla fertilizer and profession boosts are reapplied first so they stack predictably with traits
            CropTraits.modHelper.Reflection.GetMethod(dirt, "applySpeedIncreases").Invoke(planter ?? Game1.MasterPlayer);

            ApplyGrowthTraits(crop, traits);
        }

        // The farmer who planted the crop, since vanilla only applies the Agriculturist bonus for the planter
        public static Farmer GetPlanter(HoeDirt dirt, Farmer fallback)
        {
            if (dirt.modData.TryGetValue(CropTraits.PLANTER_ID_MOD_DATA_KEY, out string planterId) && long.TryParse(planterId, out long uniqueMultiplayerId))
            {
                Farmer planter = Game1.getFarmerMaybeOffline(uniqueMultiplayerId);
                if (planter is not null)
                {
                    return planter;
                }
            }

            return fallback ?? Game1.MasterPlayer;
        }

        public static int GetGrowthDays(Crop crop)
        {
            int totalDays = 0;
            for (int index = 0; index < crop.phaseDays.Count - 1; index++)
            {
                totalDays += crop.phaseDays[index];
            }

            return totalDays;
        }

        public static void RemoveGrowthDays(Crop crop, int daysToRemove)
        {
            int lastGrowthIndex = crop.phaseDays.Count - 1;

            // Each phase keeps at least one day, matching how Speed-Gro trims phases
            for (int pass = 0; pass < MAX_REMOVAL_PASSES && daysToRemove > 0; pass++)
            {
                for (int index = 0; index < lastGrowthIndex && daysToRemove > 0; index++)
                {
                    if (crop.phaseDays[index] > 1)
                    {
                        crop.phaseDays[index]--;
                        daysToRemove--;
                    }
                }
            }
        }

        public static void AddGrowthDays(Crop crop, int daysToAdd)
        {
            int lastGrowthIndex = crop.phaseDays.Count - 1;
            if (lastGrowthIndex <= 0)
            {
                return;
            }

            for (int index = 0; daysToAdd > 0; index = (index + 1) % lastGrowthIndex)
            {
                crop.phaseDays[index]++;
                daysToAdd--;
            }
        }
    }
}
