using System;
using System.Collections.Generic;
using System.Linq;
using AnglersLogbook.Framework.Models.Fish;
using StardewValley;

namespace AnglersLogbook.Framework.Utilities
{
    /// <summary>Collapses a fish's raw spawn list into the handful of rows a details page can actually show.</summary>
    public static class FishAvailabilityHelper
    {
        /// <summary>The seasons in calendar order, used to detect a year-round row.</summary>
        private static readonly Season[] AllSeasons = { Season.Spring, Season.Summer, Season.Fall, Season.Winter };

        /// <summary>The dart chance at or above which the game refuses a training rod, when no spawn entry overrides it.</summary>
        private const int TRAINING_ROD_DIFFICULTY_LIMIT = 50;

        /// <summary>Build a page-ready availability summary for one fish.</summary>
        /// <param name="fish">The aggregate fish entry.</param>
        /// <param name="includeCrabPot">Whether to include crab pot rows.</param>
        /// <param name="preferCurrentLocation">Whether to float the player's current location to the top. Leave this off if you intend to cache the result, since it makes the ordering position-dependent.</param>
        public static FishAvailability Summarize(FishEntry fish, bool includeCrabPot = true, bool preferCurrentLocation = true)
        {
            FishAvailability summary = new() { TimeSummary = FormatTimeWindows(fish.FishData), WeatherSummary = FormatWeather(fish.FishData) };

            IEnumerable<FishSpawnLocation> spawns = fish.SpawnLocations.Where(spawn => includeCrabPot || !spawn.IsCrabPot);

            // one row per location and area, since the same pair repeats once per season in the raw data
            foreach (var group in spawns.GroupBy(spawn => (spawn.LocationDisplayName, spawn.FishAreaDisplayName, spawn.IsCrabPot, spawn.IsGlobal)))
            {
                List<FishSpawnLocation> merged = group.ToList();

                // an entry with no limit anywhere makes the fish unlimited, so one unlimited row wins over any capped ones
                bool hasUnlimitedRoute = merged.Any(spawn => (spawn.Raw?.CatchLimit ?? -1) <= 0);

                summary.Rows.Add(new FishAvailabilityRow
                {
                    LocationId = merged[0].LocationId,
                    LocationDisplayName = group.Key.LocationDisplayName,
                    FishAreaDisplayName = group.Key.FishAreaDisplayName,
                    Seasons = merged.SelectMany(spawn => spawn.Seasons).Distinct().OrderBy(season => (int)season).ToList(),
                    MinFishingLevel = merged.Min(spawn => spawn.EffectiveMinFishingLevel),
                    BestChance = merged.Max(spawn => spawn.Chance),
                    RequiresMagicBait = merged.All(spawn => spawn.RequireMagicBait),
                    IsCrabPot = group.Key.IsCrabPot,
                    IsGlobal = group.Key.IsGlobal,
                    HasExtraConditions = merged.Any(spawn => HasNonSeasonCondition(spawn.Condition)),
                    CatchLimit = hasUnlimitedRoute ? -1 : merged.Max(spawn => spawn.Raw.CatchLimit),
                    IsBossFish = merged.Any(spawn => spawn.Raw?.IsBossFish is true),
                    AllowsTrainingRod = ResolveTrainingRod(merged)
                });
            }

            // where no entry states a preference the game falls back to difficulty, allowing the training rod below 50
            bool defaultAllowsTrainingRod = (fish.FishData?.DartChance ?? 0) < TRAINING_ROD_DIFFICULTY_LIMIT;
            summary.BlocksTrainingRod = summary.Rows.Count > 0 && summary.Rows.Any(row => row.AllowsTrainingRod ?? defaultAllowsTrainingRod) is false;

            summary.Rows = SortRows(summary.Rows, preferCurrentLocation);
            return summary;
        }

        /// <summary>Format a season list, collapsing a full year into a single word.</summary>
        /// <param name="seasons">The seasons to format.</param>
        public static string FormatSeasons(IReadOnlyCollection<Season> seasons)
        {
            if (seasons is null || seasons.Count is 0) { return null; }
            if (seasons.Count >= AllSeasons.Length) { return "All year"; }

            return string.Join(", ", seasons.OrderBy(season => (int)season).Select(season => Utility.getSeasonNameFromNumber((int)season)));
        }

        /// <summary>Format a row's location and area into one label.</summary>
        /// <param name="row">The row to format.</param>
        public static string FormatLocation(FishAvailabilityRow row)
        {
            if (row.IsGlobal) { return row.LocationDisplayName; }

            return string.IsNullOrWhiteSpace(row.FishAreaDisplayName) ? row.LocationDisplayName : $"{row.LocationDisplayName} ({row.FishAreaDisplayName})";
        }

        /// <summary>Format a fish's spawn windows, returning <c>null</c> when it is available all day.</summary>
        /// <param name="fishData">The parsed fish entry.</param>
        public static string FormatTimeWindows(ParsedFishData fishData)
        {
            if (fishData?.TimeWindows is null || fishData.TimeWindows.Count is 0) { return null; }
            if (fishData.TimeWindows.Count is 1 && fishData.TimeWindows[0].StartTime <= 600 && fishData.TimeWindows[0].EndTime >= 2600) { return null; }

            return string.Join(", ", fishData.TimeWindows.Select(window => $"{Game1.getTimeOfDayString(window.StartTime)} to {Game1.getTimeOfDayString(window.EndTime)}"));
        }

        /// <summary>Format a fish's weather requirement, returning <c>null</c> when weather does not matter.</summary>
        /// <param name="fishData">The parsed fish entry.</param>
        public static string FormatWeather(ParsedFishData fishData)
        {
            return fishData?.Weather switch { "sunny" => "Sunny only", "rainy" => "Rain only", _ => null };
        }


        /// <summary>Merge the training rod stance across entries, keeping an explicit allow over an explicit refusal.</summary>
        /// <param name="merged">The spawn entries sharing this row.</param>
        private static bool? ResolveTrainingRod(List<FishSpawnLocation> merged)
        {
            if (merged.Any(spawn => spawn.Raw?.CanUseTrainingRod is true)) { return true; }
            if (merged.All(spawn => spawn.Raw?.CanUseTrainingRod is false)) { return false; }

            return null;
        }

        /// <summary>Order rows so the player's current location leads, crab pots trail and everything else is alphabetical.</summary>
        private static List<FishAvailabilityRow> SortRows(List<FishAvailabilityRow> rows, bool preferCurrentLocation)
        {
            // leaving this null makes the second ordering clause constant, so the result falls through to alphabetical and becomes safe to cache
            string currentLocation = preferCurrentLocation ? Game1.currentLocation?.Name : null;

            return rows
                .OrderBy(row => row.IsGlobal)
                .ThenBy(row => row.IsCrabPot)
                .ThenBy(row => row.LocationId == currentLocation ? 0 : 1)
                .ThenBy(row => row.LocationDisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.FishAreaDisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>Check whether a condition carries anything the season summary could not express.</summary>
        private static bool HasNonSeasonCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition)) { return false; }

            foreach (string clause in condition.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] parts = clause.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length is 0) { continue; }

                string queryName = parts[0].TrimStart('!');
                if (queryName.Equals("SEASON", StringComparison.OrdinalIgnoreCase) || queryName.Equals("LOCATION_SEASON", StringComparison.OrdinalIgnoreCase)) { continue; }

                return true;
            }

            return false;
        }
    }
}
