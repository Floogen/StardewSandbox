using System.Collections.Generic;
using StardewValley.ItemTypeDefinitions;

namespace FishingLogbook.Framework.Models.Fish
{
    /// <summary>Everything known about one catchable fish, joining <c>Data/Fish</c> with every place it spawns.</summary>
    public class FishEntry
    {
        /// <summary>The item metadata, used for the display name, description and sprite. This is never null, as an entry is only created once the item has resolved.</summary>
        public ParsedItemData ItemData { get; set; }

        /// <summary>The qualified item ID (e.g. <c>(O)128</c>).</summary>
        public string QualifiedItemId => this.ItemData.QualifiedItemId;

        /// <summary>The parsed <c>Data/Fish</c> entry, or <c>null</c> if the item spawns as a fish without one (trash, forage, rings).</summary>
        public ParsedFishData FishData { get; set; }

        /// <summary>Every location and fish area this item can be caught in.</summary>
        public List<FishSpawnLocation> SpawnLocations { get; set; } = new();

        /// <summary>The translated display name.</summary>
        public string DisplayName => this.ItemData.DisplayName;
    }
}
