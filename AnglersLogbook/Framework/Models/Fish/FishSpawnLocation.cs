using System.Collections.Generic;
using StardewValley;
using StardewValley.GameData.Locations;

namespace AnglersLogbook.Framework.Models.Fish
{
    /// <summary>One place a fish can be caught, flattened from a <see cref="SpawnFishData"/> entry in <c>Data/Locations</c>.</summary>
    public class FishSpawnLocation
    {
        /// <summary>The internal location name (the <c>Data/Locations</c> key).</summary>
        public string LocationId { get; set; }

        /// <summary>The translated location name, or the <see cref="LocationId"/> if none was set.</summary>
        public string LocationDisplayName { get; set; }

        /// <summary>The fish area ID within the location, or <c>null</c> if the fish spawns in every area.</summary>
        public string FishAreaId { get; set; }

        /// <summary>The translated fish area name, if the area defined one.</summary>
        public string FishAreaDisplayName { get; set; }

        /// <summary>The seasons in which this spawn applies, resolved from <c>Season</c> and any season checks in <c>Condition</c>.</summary>
        public List<Season> Seasons { get; set; } = new();

        /// <summary>The spawn chance before buffs and modifiers.</summary>
        public float Chance { get; set; } = 1f;

        /// <summary>The fishing level required by this spawn entry alone.</summary>
        public int MinFishingLevel { get; set; }

        /// <summary>The combined fishing level requirement, taking the higher of this entry and the <c>Data/Fish</c> requirement.</summary>
        public int EffectiveMinFishingLevel { get; set; }

        /// <summary>The order this entry is checked in, where lower values are checked first.</summary>
        public int Precedence { get; set; }

        /// <summary>The raw game state query gating this spawn, if any.</summary>
        public string Condition { get; set; }

        /// <summary>Whether Magic Bait is required.</summary>
        public bool RequireMagicBait { get; set; }

        /// <summary>Whether the <c>Data/Fish</c> requirements (time, weather, depth, level) are skipped for this spawn.</summary>
        public bool IgnoreFishDataRequirements { get; set; }

        /// <summary>Whether this came from the <c>Default</c> location entry and therefore applies everywhere.</summary>
        public bool IsGlobal { get; set; }

        /// <summary>Whether this spawn represents a crab pot catch derived from a fish area rather than a <c>Fish</c> entry.</summary>
        public bool IsCrabPot { get; set; }

        /// <summary>The underlying spawn entry, for the fields not surfaced above. This is <c>null</c> for crab pot spawns.</summary>
        public SpawnFishData Raw { get; set; }
    }
}
