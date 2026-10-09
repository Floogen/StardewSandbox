using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class NightlyGrowthHelper
    {
        public static HoeDirt GetDirt(Crop crop)
        {
            if (crop is null)
            {
                return null;
            }

            if (crop.Dirt is not null)
            {
                return crop.Dirt;
            }

            if (crop.currentLocation is not null && PlantedTraitHelper.TryGetDirt(crop.currentLocation, crop.tilePosition, out HoeDirt dirt) && dirt.crop == crop)
            {
                return dirt;
            }

            return null;
        }

        // Mirrors Crop.IsInSeason without the trait patch, which is what the game kills outdoor crops by
        public static bool IsInSeasonIgnoringTraits(Crop crop, GameLocation location)
        {
            if (location.SeedsIgnoreSeasonsHere())
            {
                return true;
            }

            return crop.GetData()?.Seasons?.Contains(location.GetSeason()) ?? false;
        }

        public static int GetOutOfSeasonNights(HoeDirt dirt)
        {
            if (dirt.modData.TryGetValue(NightlyGrowthManager.OUT_OF_SEASON_NIGHTS_MOD_DATA_KEY, out string value) && int.TryParse(value, out int nights))
            {
                return nights;
            }

            return 0;
        }

        // Counted before the game checks the season, so a crop allowed three nights survives all three
        public static void CountOutOfSeasonNight(Crop crop, HoeDirt dirt, TraitSet traits)
        {
            GameLocation location = crop.currentLocation;
            if (location is null || location.IsOutdoors is false || CropTraits.traitManager.GetOutOfSeasonNights(traits) <= 0)
            {
                return;
            }

            if (IsInSeasonIgnoringTraits(crop, location))
            {
                dirt.modData.Remove(NightlyGrowthManager.OUT_OF_SEASON_NIGHTS_MOD_DATA_KEY);
                return;
            }

            dirt.modData[NightlyGrowthManager.OUT_OF_SEASON_NIGHTS_MOD_DATA_KEY] = (GetOutOfSeasonNights(dirt) + 1).ToString();
        }

        public static int GetDaysLeftReady(HoeDirt dirt)
        {
            if (dirt.modData.TryGetValue(NightlyGrowthManager.DAYS_LEFT_READY_MOD_DATA_KEY, out string value) && int.TryParse(value, out int days))
            {
                return days;
            }

            return 0;
        }

        // Counts each full day a crop with a trait like Fragile sat ready, clearing the count once it's no longer ready
        public static void CountDayLeftReady(HoeDirt dirt, TraitSet traits, bool wasLeftReady)
        {
            if (CropTraits.traitManager.LosesQualityWhenLeftReady(traits) is false || dirt.readyForHarvest() is false)
            {
                dirt.modData.Remove(NightlyGrowthManager.DAYS_LEFT_READY_MOD_DATA_KEY);
                return;
            }

            // A crop that only became ready tonight hasn't sat through a full day yet
            if (wasLeftReady)
            {
                dirt.modData[NightlyGrowthManager.DAYS_LEFT_READY_MOD_DATA_KEY] = (GetDaysLeftReady(dirt) + 1).ToString();
            }
        }

        public static void ApplyPlan(Crop crop, CropNightState nightState)
        {
            NightlyGrowthPlan plan = nightState.Plan;
            if (plan is null)
            {
                return;
            }

            if (nightState.IsGrowing is false)
            {
                if (plan.LosesGrowthWhenDry)
                {
                    RegressGrowth(crop, nightState.Dirt);
                }

                return;
            }

            // A skipped night undo the growth the game just applied
            if (plan.SkipsGrowth)
            {
                crop.currentPhase.Value = nightState.CurrentPhase;
                crop.dayOfCurrentPhase.Value = nightState.DayOfCurrentPhase;
            }

            for (int day = 0; day < plan.ExtraGrowthDays; day++)
            {
                AdvanceGrowth(crop, nightState.Dirt);
            }
        }

        // Matches the growth step in Crop.newDay, without its giant crop roll or maturity dialogue
        public static void AdvanceGrowth(Crop crop, HoeDirt dirt)
        {
            if (dirt.readyForHarvest() || crop.phaseDays.Count == 0)
            {
                return;
            }

            int phaseLength = crop.phaseDays[Math.Min(crop.phaseDays.Count - 1, crop.currentPhase.Value)];
            if (crop.fullyGrown.Value)
            {
                crop.dayOfCurrentPhase.Value--;
            }
            else
            {
                crop.dayOfCurrentPhase.Value = Math.Min(crop.dayOfCurrentPhase.Value + 1, phaseLength);
            }

            if (crop.dayOfCurrentPhase.Value >= phaseLength && crop.currentPhase.Value < crop.phaseDays.Count - 1)
            {
                crop.currentPhase.Value++;
                crop.dayOfCurrentPhase.Value = 0;
            }

            while (crop.currentPhase.Value < crop.phaseDays.Count - 1 && crop.phaseDays[crop.currentPhase.Value] <= 0)
            {
                crop.currentPhase.Value++;
            }
        }

        // Steps back one day of growth, never before planting and never on a crop that's ready to harvest
        public static void RegressGrowth(Crop crop, HoeDirt dirt)
        {
            if (dirt.readyForHarvest() || crop.phaseDays.Count == 0)
            {
                return;
            }

            // Regrowing crops count down to their next harvest instead
            if (crop.fullyGrown.Value)
            {
                int regrowDays = Math.Max(crop.GetData()?.RegrowDays ?? 0, 0);
                crop.dayOfCurrentPhase.Value = Math.Min(crop.dayOfCurrentPhase.Value + 1, regrowDays);
                return;
            }

            if (crop.dayOfCurrentPhase.Value > 0)
            {
                crop.dayOfCurrentPhase.Value--;
                return;
            }

            if (crop.currentPhase.Value <= 0)
            {
                return;
            }

            // Phases with no days are skipped going forward, so they're skipped going back too
            int previousPhase = crop.currentPhase.Value - 1;
            while (previousPhase > 0 && crop.phaseDays[previousPhase] <= 0)
            {
                previousPhase--;
            }

            crop.currentPhase.Value = previousPhase;
            crop.dayOfCurrentPhase.Value = Math.Max(crop.phaseDays[previousPhase] - 1, 0);
        }
    }
}
