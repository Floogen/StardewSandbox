using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FishingLogbook.Framework.Models.Fish;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Objects;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;

namespace FishingLogbook.Framework.Utilities
{
    /// <summary>Joins <c>Data/Fish</c> and <c>Data/Locations</c> into a single queryable view of every catchable fish.</summary>
    public static class FishDataHelper
    {
        /// <summary>The <c>Data/Locations</c> key whose spawn lists are merged into every other location.</summary>
        private const string DefaultLocationKey = "Default";

        /// <summary>The label shown for spawns inherited from the <c>Default</c> entry, which apply in every location.</summary>
        private static string GlobalLocationName => ModEntry.translation.Get("ui.location.anywhere");

        /// <summary>The item query which pulls another location's fish list into this one.</summary>
        private const string LocationFishQuery = "LOCATION_FISH";

        /// <summary>The key prefix shared by every farm type's entry, of which only one applies to any given save.</summary>
        private const string FarmKeyPrefix = "Farm_";

        /// <summary>A custom field a content pack can set on its location entry to keep it out of the logbook.</summary>
        private const string HideFromLogbookField = "PeacefulEnd.Parchment.FishingLogbook_Hide";

        /// <summary>Locations which exist in the data but are never somewhere the player goes fishing on purpose.</summary>
        private static readonly HashSet<string> ExcludedLocationIds = new(StringComparer.OrdinalIgnoreCase) { "fishingGame", "Temp" };

        /// <summary>Every season, used when a spawn entry has no season restriction.</summary>
        private static readonly Season[] AllSeasons = { Season.Spring, Season.Summer, Season.Fall, Season.Winter };

        /// <summary>The cached result of the default <see cref="GetAllFish"/> query, or <c>null</c> if it needs rebuilding.</summary>
        private static List<FishEntry> _cachedFish;


        /// <summary>Build the full fish list from the currently loaded game data, including any mod edits.</summary>
        /// <param name="includeCrabPotFish">Whether to derive crab pot spawns from each location's fish areas.</param>
        /// <param name="includeGlobalSpawns">Whether to include the <c>Default</c> location's spawn list, recorded once as a single "Anywhere" row rather than duplicated across every location.</param>
        /// <param name="includeItemsWithoutFishData">Whether to keep spawn entries whose item has no <c>Data/Fish</c> entry, such as trash, rings or forage.</param>
        /// <param name="excludeNonCollectableFish">Whether to drop items flagged <c>ExcludeFromFishingCollection</c> in <c>Data/Objects</c>, matching what the vanilla collections tab shows.</param>
        /// <param name="excludeLocation">An optional extra test for locations to leave out, on top of the built-in exclusions.</param>
        public static List<FishEntry> GetAllFish(bool includeCrabPotFish = true, bool includeGlobalSpawns = true, bool includeItemsWithoutFishData = false, bool excludeNonCollectableFish = true, Func<string, LocationData, bool> excludeLocation = null)
        {
            // Only the default query is cached, since it is rebuilt on every refresh
            bool isCacheable = includeCrabPotFish && includeGlobalSpawns && includeItemsWithoutFishData is false && excludeNonCollectableFish && excludeLocation is null;
            if (isCacheable is false)
            {
                return BuildAllFish(includeCrabPotFish, includeGlobalSpawns, includeItemsWithoutFishData, excludeNonCollectableFish, excludeLocation);
            }

            if (_cachedFish is null)
            {
                _cachedFish = BuildAllFish(includeCrabPotFish, includeGlobalSpawns, includeItemsWithoutFishData, excludeNonCollectableFish, excludeLocation);
            }

            return _cachedFish;
        }

        /// <summary>Discards the cached fish list, so the next default query rebuilds it from the current game data.</summary>
        public static void ClearCache()
        {
            _cachedFish = null;
        }

        /// <summary>Builds the fish list from the currently loaded game data, without consulting the cache.</summary>
        private static List<FishEntry> BuildAllFish(bool includeCrabPotFish, bool includeGlobalSpawns, bool includeItemsWithoutFishData, bool excludeNonCollectableFish, Func<string, LocationData, bool> excludeLocation)
        {
            Dictionary<string, string> rawFish = DataLoader.Fish(Game1.content);
            Dictionary<string, LocationData> locations = DataLoader.Locations(Game1.content);

            // parse Data/Fish once, keyed by unqualified ID
            Dictionary<string, ParsedFishData> parsedFish = new();
            foreach ((string fishId, string rawEntry) in rawFish)
            {
                if (TryParseFishData(fishId, rawEntry, out ParsedFishData parsed)) { parsedFish[fishId] = parsed; }
            }

            Dictionary<string, FishEntry> entries = new();
            Dictionary<string, string> worldMapNames = BuildWorldMapNames();
            locations.TryGetValue(DefaultLocationKey, out LocationData defaultLocation);

            // resolved through the live farm rather than the key, so custom farm types work without knowing their naming rules
            LocationData currentFarmData = Game1.getLocationFromName("Farm")?.GetData();

            foreach ((string locationId, LocationData locationData) in locations)
            {
                if (locationId == DefaultLocationKey) { continue; }
                if (ShouldSkipLocation(locationId, locationData, currentFarmData, excludeLocation)) { continue; }

                string locationName = GetLocationDisplayName(locationId, locationData, worldMapNames);

                // rod spawns declared on the location itself
                foreach (SpawnFishData spawn in locationData.Fish ?? Enumerable.Empty<SpawnFishData>())
                {
                    AddRodSpawn(entries, parsedFish, locations, locationId, locationName, locationData, spawn, isGlobal: false, isInherited: false, includeItemsWithoutFishData, new HashSet<string> { locationId });
                }

                // crab pot catches, which come from the fish area's water types rather than a Fish entry
                if (includeCrabPotFish)
                {
                    AddCrabPotSpawns(entries, parsedFish, locationId, locationName, locationData);
                }
            }

            // the Default entry applies everywhere, so record it once as its own row rather than duplicating it per location
            if (includeGlobalSpawns && defaultLocation is not null)
            {
                foreach (SpawnFishData spawn in defaultLocation.Fish ?? Enumerable.Empty<SpawnFishData>())
                {
                    AddRodSpawn(entries, parsedFish, locations, DefaultLocationKey, GlobalLocationName, defaultLocation, spawn, isGlobal: true, isInherited: false, includeItemsWithoutFishData, new HashSet<string> { DefaultLocationKey });
                }
            }

            return entries.Values
                .Where(entry => excludeNonCollectableFish is false || IsInFishingCollection(entry.ItemData))
                .OrderBy(entry => entry.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>Parse one <c>Data/Fish</c> value into a model.</summary>
        /// <param name="fishId">The unqualified item ID which keys the entry.</param>
        /// <param name="rawEntry">The slash-delimited entry value.</param>
        /// <param name="parsed">The parsed entry, if valid.</param>
        public static bool TryParseFishData(string fishId, string rawEntry, out ParsedFishData parsed)
        {
            parsed = null;
            if (string.IsNullOrWhiteSpace(rawEntry)) { return false; }

            string[] fields = rawEntry.Split('/');
            if (fields.Length < 2) { return false; }

            // trap fish use an entirely different field layout
            if (fields[1].Trim().Equals("trap", StringComparison.OrdinalIgnoreCase))
            {
                parsed = new ParsedFishData { ItemId = fishId, InternalName = fields[0], IsTrapFish = true, TrapChance = ParseFloat(GetField(fields, 2)), TrapWaterType = GetField(fields, 4)?.Trim().ToLowerInvariant() ?? "freshwater", MinSize = ParseInt(GetField(fields, 5)), MaxSize = ParseInt(GetField(fields, 6)) };
                return true;
            }

            if (fields.Length < 13) { return false; }

            parsed = new ParsedFishData
            {
                ItemId = fishId,
                InternalName = fields[0],
                IsTrapFish = false,
                DartChance = ParseInt(GetField(fields, 1)),
                DartBehavior = GetField(fields, 2)?.Trim().ToLowerInvariant() ?? "mixed",
                MinSize = ParseInt(GetField(fields, 3)),
                MaxSize = ParseInt(GetField(fields, 4)),
                TimeWindows = ParseTimeWindows(GetField(fields, 5)),
                Weather = GetField(fields, 7)?.Trim().ToLowerInvariant() ?? "both",
                MaxDepth = ParseInt(GetField(fields, 9)),
                BaseChance = ParseFloat(GetField(fields, 10)),
                DepthMultiplier = ParseFloat(GetField(fields, 11)),
                MinFishingLevel = ParseInt(GetField(fields, 12)),
                CanBeTutorialCatch = ParseBool(GetField(fields, 13))
            };
            return true;
        }

        /// <summary>Format a fish's spawn windows into a human-readable string such as "6:00am to 7:00pm".</summary>
        /// <param name="fishData">The parsed fish entry.</param>
        public static string FormatTimeWindows(ParsedFishData fishData)
        {
            if (fishData?.TimeWindows is null || fishData.TimeWindows.Count is 0) { return I18n_AnyTime(); }
            if (fishData.TimeWindows.Count is 1 && fishData.TimeWindows[0].StartTime <= 600 && fishData.TimeWindows[0].EndTime >= 2600) { return I18n_AnyTime(); }

            return string.Join(", ", fishData.TimeWindows.Select(window => ModEntry.translation.Get("ui.time.rangeCompact", new { start = Game1.getTimeOfDayString(window.StartTime), end = Game1.getTimeOfDayString(window.EndTime) }).ToString()));
        }

        /// <summary>Get an approximate catch chance for display purposes, mirroring the shape of <c>GameLocation.CheckGenericFishRequirements</c>.</summary>
        /// <param name="fishData">The parsed <c>Data/Fish</c> entry, or <c>null</c> to use the spawn chance alone.</param>
        /// <param name="spawn">The location spawn entry.</param>
        /// <param name="fishingLevel">The player's fishing level.</param>
        /// <param name="bobberDepth">The distance from shore in tiles where the bobber landed.</param>
        public static float GetApproximateChance(ParsedFishData fishData, FishSpawnLocation spawn, int fishingLevel, int bobberDepth = 4)
        {
            float chance = spawn?.Chance ?? 1f;
            if (fishData is null || fishData.IsTrapFish) { return Math.Clamp(chance, 0f, 1f); }
            if (spawn?.IgnoreFishDataRequirements is true) { return Math.Clamp(chance, 0f, 1f); }

            float dataChance = fishData.BaseChance + fishingLevel / 50f;
            dataChance = Math.Min(dataChance, 0.9f);
            if (bobberDepth < fishData.MaxDepth) { dataChance -= dataChance * fishData.DepthMultiplier * (fishData.MaxDepth - bobberDepth); }

            return Math.Clamp(chance * dataChance, 0f, 1f);
        }


        /// <summary>Flatten one rod spawn entry into the aggregate lookup, resolving its item ID or item query.</summary>
        private static void AddRodSpawn(Dictionary<string, FishEntry> entries, Dictionary<string, ParsedFishData> parsedFish, Dictionary<string, LocationData> locations, string locationId, string locationName, LocationData locationData, SpawnFishData spawn, bool isGlobal, bool isInherited, bool includeItemsWithoutFishData, HashSet<string> visitedLocations)
        {
            // a LOCATION_FISH query borrows another location's whole list, so expand it structurally rather than resolving it to one random item
            if (TryGetLocationFishSource(spawn.ItemId, out string sourceLocationId))
            {
                ExpandLocationFish(entries, parsedFish, locations, locationId, locationName, sourceLocationId, isGlobal, includeItemsWithoutFishData, visitedLocations);
                return;
            }

            foreach (ParsedItemData itemData in ResolveItems(spawn))
            {
                parsedFish.TryGetValue(itemData.ItemId, out ParsedFishData fishData);
                if (fishData is null && !includeItemsWithoutFishData) { continue; }

                FishEntry entry = GetOrCreateEntry(entries, itemData, fishData);
                int dataLevel = spawn.IgnoreFishDataRequirements || fishData is null ? 0 : fishData.MinFishingLevel;

                entry.SpawnLocations.Add(new FishSpawnLocation
                {
                    LocationId = locationId,
                    LocationDisplayName = locationName,
                    // an inherited entry's fish area belongs to the source location, so it means nothing here
                    FishAreaId = isInherited ? null : spawn.FishAreaId,
                    FishAreaDisplayName = isInherited ? null : GetFishAreaName(locationData, spawn.FishAreaId),
                    Seasons = ResolveSeasons(spawn.Season, spawn.Condition),
                    Chance = spawn.Chance,
                    MinFishingLevel = spawn.MinFishingLevel,
                    EffectiveMinFishingLevel = Math.Max(spawn.MinFishingLevel, dataLevel),
                    Precedence = spawn.Precedence,
                    Condition = spawn.Condition,
                    RequireMagicBait = spawn.RequireMagicBait,
                    IgnoreFishDataRequirements = spawn.IgnoreFishDataRequirements,
                    IsGlobal = isGlobal,
                    IsCrabPot = false,
                    Raw = spawn
                });
            }
        }

        /// <summary>Check whether an item counts toward the fishing collection, per its <c>Data/Objects</c> entry.</summary>
        /// <param name="itemData">The item metadata.</param>
        private static bool IsInFishingCollection(ParsedItemData itemData)
        {
            return itemData?.RawData is not ObjectData objectData || objectData.ExcludeFromFishingCollection is false;
        }

        /// <summary>Check whether a location should be left off the logbook entirely.</summary>
        /// <param name="locationId">The internal location name.</param>
        /// <param name="locationData">The location's data entry.</param>
        /// <param name="currentFarmData">The data entry the save's own farm resolved to, or <c>null</c> if no save is loaded.</param>
        /// <param name="excludeLocation">An optional extra test supplied by the caller.</param>
        private static bool ShouldSkipLocation(string locationId, LocationData locationData, LocationData currentFarmData, Func<string, LocationData, bool> excludeLocation)
        {
            // minigame and scratch locations exist in the data but are never a real fishing spot
            if (ExcludedLocationIds.Contains(locationId)) { return true; }

            // every farm type has its own entry, so keep the one this save actually uses and drop the rest
            if (locationId.StartsWith(FarmKeyPrefix, StringComparison.OrdinalIgnoreCase) && currentFarmData is not null && ReferenceEquals(locationData, currentFarmData) is false) { return true; }

            // lets a content pack keep its own utility locations out without needing a change here
            if (locationData?.CustomFields is not null && locationData.CustomFields.ContainsKey(HideFromLogbookField)) { return true; }

            return excludeLocation is not null && excludeLocation(locationId, locationData);
        }

        /// <summary>Check whether a spawn's item ID is a <c>LOCATION_FISH</c> query, and pull out the location it borrows from.</summary>
        /// <param name="itemId">The spawn entry's item ID.</param>
        /// <param name="sourceLocationId">The internal name of the location being borrowed from.</param>
        private static bool TryGetLocationFishSource(string itemId, out string sourceLocationId)
        {
            sourceLocationId = null;
            if (string.IsNullOrWhiteSpace(itemId)) { return false; }

            // the query takes the form: LOCATION_FISH <location> <bobber x> <bobber y> <depth>
            string[] parts = itemId.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !parts[0].Equals(LocationFishQuery, StringComparison.OrdinalIgnoreCase)) { return false; }

            sourceLocationId = parts[1].Trim('"');
            return true;
        }

        /// <summary>Pull a borrowed location's inheritable spawns into the location doing the fishing.</summary>
        /// <param name="visitedLocations">The locations already walked on this branch, used to stop a circular reference.</param>
        private static void ExpandLocationFish(Dictionary<string, FishEntry> entries, Dictionary<string, ParsedFishData> parsedFish, Dictionary<string, LocationData> locations, string locationId, string locationName, string sourceLocationId, bool isGlobal, bool includeItemsWithoutFishData, HashSet<string> visitedLocations)
        {
            // the game logs an error and returns nothing on a circular reference, so stop rather than recursing forever
            if (visitedLocations.Add(sourceLocationId) is false) { return; }

            if (locations.TryGetValue(sourceLocationId, out LocationData sourceData))
            {
                foreach (SpawnFishData sourceSpawn in sourceData.Fish ?? Enumerable.Empty<SpawnFishData>())
                {
                    // only entries flagged inheritable are eligible to be borrowed
                    if (sourceSpawn.CanBeInherited is false) { continue; }

                    // the row belongs to the location being fished, but keeps the source entry's own requirements
                    AddRodSpawn(entries, parsedFish, locations, locationId, locationName, sourceData, sourceSpawn, isGlobal, isInherited: true, includeItemsWithoutFishData, visitedLocations);
                }
            }

            visitedLocations.Remove(sourceLocationId);
        }

        /// <summary>Derive crab pot spawns by matching each fish area's water types against field 4 of the trap entries.</summary>
        private static void AddCrabPotSpawns(Dictionary<string, FishEntry> entries, Dictionary<string, ParsedFishData> parsedFish, string locationId, string locationName, LocationData locationData)
        {
            if (locationData.FishAreas is null || locationData.FishAreas.Count is 0) { return; }

            foreach ((string areaId, FishAreaData areaData) in locationData.FishAreas)
            {
                // an omitted CrabPotFishTypes defaults to freshwater
                List<string> waterTypes = areaData.CrabPotFishTypes?.Count > 0 ? areaData.CrabPotFishTypes : new List<string> { "freshwater" };

                foreach (ParsedFishData fishData in parsedFish.Values.Where(fish => fish.IsTrapFish))
                {
                    if (!waterTypes.Any(type => type.Equals(fishData.TrapWaterType, StringComparison.OrdinalIgnoreCase))) { continue; }

                    ParsedItemData itemData = ItemRegistry.GetData(fishData.ItemId);
                    if (itemData is null) { continue; }

                    FishEntry entry = GetOrCreateEntry(entries, itemData, fishData);
                    entry.SpawnLocations.Add(new FishSpawnLocation { LocationId = locationId, LocationDisplayName = locationName, FishAreaId = areaId, FishAreaDisplayName = GetDisplayName(areaData.DisplayName, areaId), Seasons = AllSeasons.ToList(), Chance = fishData.TrapChance, EffectiveMinFishingLevel = 0, IsCrabPot = true });
                }
            }
        }

        /// <summary>Get the aggregate entry for an item, creating it if this is the first spawn seen.</summary>
        private static FishEntry GetOrCreateEntry(Dictionary<string, FishEntry> entries, ParsedItemData itemData, ParsedFishData fishData)
        {
            if (!entries.TryGetValue(itemData.QualifiedItemId, out FishEntry entry))
            {
                entry = new FishEntry { ItemData = itemData, FishData = fishData };
                entries[itemData.QualifiedItemId] = entry;
            }

            entry.FishData ??= fishData;
            return entry;
        }

        /// <summary>Resolve a spawn entry's item ID, expanding item queries and random ID lists into concrete items.</summary>
        private static IEnumerable<ParsedItemData> ResolveItems(SpawnFishData spawn)
        {
            List<string> itemIds = new();
            if (spawn.RandomItemId?.Count > 0) { itemIds.AddRange(spawn.RandomItemId); }
            else if (!string.IsNullOrWhiteSpace(spawn.ItemId)) { itemIds.Add(spawn.ItemId); }

            foreach (string itemId in itemIds)
            {
                // the common case is a plain qualified or unqualified ID
                ParsedItemData direct = ItemRegistry.GetData(itemId);
                if (direct is not null)
                {
                    yield return direct;
                    continue;
                }

                // otherwise treat it as an item query, which may expand to many items
                foreach (ItemQueryResult result in ItemQueryResolver.TryResolve(itemId, new ItemQueryContext(), ItemQuerySearchMode.AllOfTypeItem))
                {
                    if (result.Item is null) { continue; }

                    ParsedItemData resolved = ItemRegistry.GetData(result.Item.QualifiedItemId);
                    if (resolved is not null) { yield return resolved; }
                }
            }
        }

        /// <summary>Work out which seasons a spawn applies to, falling back to reading season checks out of the condition.</summary>
        /// <param name="season">The explicit season field, if set.</param>
        /// <param name="condition">The game state query gating the spawn, if any.</param>
        private static List<Season> ResolveSeasons(Season? season, string condition)
        {
            if (season.HasValue) { return new List<Season> { season.Value }; }
            if (string.IsNullOrWhiteSpace(condition)) { return AllSeasons.ToList(); }

            HashSet<Season> allowed = new(AllSeasons);
            foreach (string clause in condition.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] parts = clause.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length is 0) { continue; }

                bool negated = parts[0].StartsWith('!');
                string queryName = negated ? parts[0][1..] : parts[0];

                int firstSeasonIndex = queryName.Equals("SEASON", StringComparison.OrdinalIgnoreCase) ? 1 : queryName.Equals("LOCATION_SEASON", StringComparison.OrdinalIgnoreCase) ? 2 : -1;
                if (firstSeasonIndex is -1) { continue; }

                HashSet<Season> named = new();
                for (int i = firstSeasonIndex; i < parts.Length; i++)
                {
                    if (Enum.TryParse(parts[i], ignoreCase: true, out Season parsedSeason)) { named.Add(parsedSeason); }
                }

                if (named.Count is 0) { continue; }
                if (negated) { allowed.ExceptWith(named); } else { allowed.IntersectWith(named); }
            }

            return allowed.ToList();
        }

        /// <summary>Parse field 5 of a rod fish entry into its spawn windows.</summary>
        private static List<FishTimeWindow> ParseTimeWindows(string field)
        {
            List<FishTimeWindow> windows = new();
            if (string.IsNullOrWhiteSpace(field)) { return windows; }

            string[] parts = field.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                if (int.TryParse(parts[i], out int start) && int.TryParse(parts[i + 1], out int end)) { windows.Add(new FishTimeWindow(start, end)); }
            }

            return windows;
        }

        /// <summary>Resolve a location's display name, falling back for the many locations that never set one.</summary>
        /// <param name="locationId">The internal location name.</param>
        /// <param name="locationData">The location's data entry.</param>
        /// <param name="worldMapNames">The location to world map name lookup from <see cref="BuildWorldMapNames"/>.</param>
        private static string GetLocationDisplayName(string locationId, LocationData locationData, Dictionary<string, string> worldMapNames)
        {
            // the data asset is the intended source, but DisplayName is optional and plenty of entries omit it
            string fromData = ParseTokenizable(locationData?.DisplayName);
            if (string.IsNullOrWhiteSpace(fromData) is false) { return fromData; }

            // content packs written before 1.6 set the name as a map property instead
            GameLocation location = Game1.getLocationFromName(locationId);
            if (location is not null)
            {
                string fromMap = ParseTokenizable(location.getMapProperty("DisplayName"));
                if (string.IsNullOrWhiteSpace(fromMap) is false) { return fromMap; }
            }

            // some packs name the place only on the world map, which is coarser but still better than an internal ID
            if (worldMapNames is not null && worldMapNames.TryGetValue(locationId, out string fromWorldMap)) { return fromWorldMap; }

            return PrettifyLocationId(locationId);
        }

        /// <summary>Build a lookup of internal location name to the name the world map shows for it.</summary>
        private static Dictionary<string, string> BuildWorldMapNames()
        {
            Dictionary<string, string> names = new(StringComparer.OrdinalIgnoreCase);

            foreach (var region in DataLoader.WorldMap(Game1.content).Values)
            {
                if (region.MapAreas is null) { continue; }

                foreach (var area in region.MapAreas)
                {
                    // the scroll text along the bottom of the map is the area's short name, with the hover tooltip behind it
                    string areaName = ParseTokenizable(area.ScrollText);
                    if (string.IsNullOrWhiteSpace(areaName) && area.Tooltips is not null)
                    {
                        foreach (var tooltip in area.Tooltips)
                        {
                            areaName = ParseTokenizable(tooltip.Text);
                            if (string.IsNullOrWhiteSpace(areaName) is false) { break; }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(areaName) || area.WorldPositions is null) { continue; }

                    foreach (var position in area.WorldPositions)
                    {
                        // a position can override the area's scroll text with something more specific, such as Mountain Lake
                        string positionName = ParseTokenizable(position.ScrollText);
                        string name = string.IsNullOrWhiteSpace(positionName) ? areaName : positionName;

                        if (string.IsNullOrWhiteSpace(position.LocationName) is false) { names.TryAdd(position.LocationName, name); }
                        if (position.LocationNames is null) { continue; }

                        foreach (string locationName in position.LocationNames)
                        {
                            if (string.IsNullOrWhiteSpace(locationName) is false) { names.TryAdd(locationName, name); }
                        }
                    }
                }
            }

            return names;
        }

        /// <summary>Parse a tokenizable string, treating a blank value as absent so callers can chain fallbacks.</summary>
        /// <param name="tokenizableString">The raw tokenizable string.</param>
        private static string ParseTokenizable(string tokenizableString)
        {
            return string.IsNullOrWhiteSpace(tokenizableString) ? null : TokenParser.ParseText(tokenizableString);
        }

        /// <summary>Turn an internal location name into something readable, for locations that supply no display name at all.</summary>
        /// <param name="locationId">The internal location name.</param>
        private static string PrettifyLocationId(string locationId)
        {
            if (string.IsNullOrWhiteSpace(locationId)) { return locationId; }

            // drop a namespaced mod prefix such as "Author.ModName_SunkenGrotto", which would otherwise swamp the row
            int separatorIndex = locationId.IndexOf('_');
            string trimmed = separatorIndex > 0 && locationId.LastIndexOf('.', separatorIndex) > 0 ? locationId[(separatorIndex + 1)..] : locationId;

            // split camel case and underscores so "IslandSouthEast" reads as "Island South East"
            return Regex.Replace(trimmed.Replace('_', ' '), "(?<=[a-z0-9])(?=[A-Z])", " ").Trim();
        }

        /// <summary>Resolve a tokenizable display name, falling back to an ID when it is missing.</summary>
        private static string GetDisplayName(string tokenizableString, string fallback)
        {
            if (string.IsNullOrWhiteSpace(tokenizableString)) { return fallback; }

            string parsed = TokenParser.ParseText(tokenizableString);
            return string.IsNullOrWhiteSpace(parsed) ? fallback : parsed;
        }

        /// <summary>Look up a fish area's translated name within a location.</summary>
        private static string GetFishAreaName(LocationData locationData, string fishAreaId)
        {
            if (string.IsNullOrWhiteSpace(fishAreaId)) { return null; }
            if (locationData?.FishAreas is null || !locationData.FishAreas.TryGetValue(fishAreaId, out FishAreaData areaData)) { return fishAreaId; }

            return GetDisplayName(areaData.DisplayName, fishAreaId);
        }

        /// <summary>Get a field by index, or <c>null</c> if the entry was short.</summary>
        private static string GetField(string[] fields, int index) => index < fields.Length ? fields[index] : null;

        private static int ParseInt(string value) => int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0;

        private static float ParseFloat(string value) => float.TryParse(value?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f;

        private static bool ParseBool(string value) => bool.TryParse(value?.Trim(), out bool parsed) && parsed;

        /// <summary>The translated "any time" label, shown when a fish is available all day.</summary>
        private static string I18n_AnyTime() => ModEntry.translation.Get("ui.details.anyTime");
    }
}
