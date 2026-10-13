using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;

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
            if (crop is null)
            {
                return;
            }

            ApplyGrowthTraits(crop.phaseDays, crop.GetData(), traits);
        }

        public static void ApplyGrowthTraits(IList<int> phaseDays, CropData cropData, TraitSet traits)
        {
            if (phaseDays is null || traits is null || phaseDays.Count < 2)
            {
                return;
            }

            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(traits))
            {
                trait.ModifyGrowth(phaseDays, cropData, tier);
            }
        }

        public static int GetBaseGrowthDays(CropData cropData)
        {
            return GetGrowthDays(BuildPhaseDays(cropData));
        }

        // Matches the vanilla seed description, so fertilizer and profession boosts are left out
        public static int GetEstimatedGrowthDays(CropData cropData, TraitSet traits)
        {
            List<int> phaseDays = BuildPhaseDays(cropData);
            ApplyGrowthTraits(phaseDays, cropData, traits);

            return GetGrowthDays(phaseDays);
        }

        // Data phases plus the final phase, the same shape as a planted crop's phaseDays
        public static List<int> BuildPhaseDays(CropData cropData)
        {
            List<int> phaseDays = new List<int>();
            if (cropData?.DaysInPhase is null || cropData.DaysInPhase.Count == 0)
            {
                return phaseDays;
            }

            phaseDays.AddRange(cropData.DaysInPhase);
            phaseDays.Add(FINAL_PHASE_LENGTH);

            return phaseDays;
        }

        // Rebuilds phases from crop data so growth traits never double up or get wiped by vanilla speed increases
        public static void ReapplyGrowthTraits(HoeDirt dirt, TraitSet traits, Farmer planter)
        {
            Crop crop = dirt.crop;
            List<int> phaseDays = BuildPhaseDays(crop?.GetData());
            if (phaseDays.Count == 0)
            {
                return;
            }

            crop.phaseDays.Clear();
            foreach (int days in phaseDays)
            {
                crop.phaseDays.Add(days);
            }

            // Vanilla fertilizer and profession boosts are reapplied first so they stack predictably with traits
            dirt.applySpeedIncreases(planter ?? Game1.MasterPlayer);

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

        public static int GetGrowthDays(IList<int> phaseDays)
        {
            int totalDays = 0;
            for (int index = 0; index < phaseDays.Count - 1; index++)
            {
                totalDays += phaseDays[index];
            }

            return totalDays;
        }

        // Sends a crop back to the growth stage halfway through, so it regrows in about half its growth time using its normal sprites
        public static void ResetToHalfwayGrowth(Crop crop)
        {
            int totalDays = GetGrowthDays(crop.phaseDays);
            int elapsedDays = totalDays - Math.Max((int)Math.Ceiling(totalDays / 2.0), 1);

            int phase = 0;
            while (phase < crop.phaseDays.Count - 2 && elapsedDays >= crop.phaseDays[phase])
            {
                elapsedDays -= crop.phaseDays[phase];
                phase++;
            }

            crop.fullyGrown.Value = false;
            crop.currentPhase.Value = phase;
            crop.dayOfCurrentPhase.Value = Math.Max(elapsedDays, 0);
            crop.updateDrawMath(crop.tilePosition);
        }

        public static void RemoveGrowthDays(IList<int> phaseDays, int daysToRemove)
        {
            int lastGrowthIndex = phaseDays.Count - 1;

            // Each phase keeps at least one day, matching how Speed-Gro trims phases
            for (int pass = 0; pass < MAX_REMOVAL_PASSES && daysToRemove > 0; pass++)
            {
                for (int index = 0; index < lastGrowthIndex && daysToRemove > 0; index++)
                {
                    if (phaseDays[index] > 1)
                    {
                        phaseDays[index]--;
                        daysToRemove--;
                    }
                }
            }
        }

        public static void AddGrowthDays(IList<int> phaseDays, int daysToAdd)
        {
            int lastGrowthIndex = phaseDays.Count - 1;
            if (lastGrowthIndex <= 0)
            {
                return;
            }

            for (int index = 0; daysToAdd > 0; index = (index + 1) % lastGrowthIndex)
            {
                phaseDays[index]++;
                daysToAdd--;
            }
        }
    }
}
