using System.Collections.Generic;
using StardewValley;

namespace AnglersLogbook.Framework.Models.Fish
{
    /// <summary>One row of a fish details page, merging every spawn entry that shares a location and fish area.</summary>
    public class FishAvailabilityRow
    {
        /// <summary>The internal location name, used for sorting against the player's current location.</summary>
        public string LocationId { get; set; }

        /// <summary>The translated location name.</summary>
        public string LocationDisplayName { get; set; }

        /// <summary>The translated fish area name, or <c>null</c> when the fish spawns anywhere in the location.</summary>
        public string FishAreaDisplayName { get; set; }

        /// <summary>The seasons covered by this row, in calendar order.</summary>
        public List<Season> Seasons { get; set; } = new();

        /// <summary>The lowest fishing level across the merged entries, since that is when the fish first becomes reachable here.</summary>
        public int MinFishingLevel { get; set; }

        /// <summary>The highest base spawn chance across the merged entries.</summary>
        public float BestChance { get; set; }

        /// <summary>Whether every merged entry requires Magic Bait, meaning the fish is unreachable here without it.</summary>
        public bool RequiresMagicBait { get; set; }

        /// <summary>Whether this row is a crab pot catch rather than a rod catch.</summary>
        public bool IsCrabPot { get; set; }

        /// <summary>Whether this row came from the <c>Default</c> entry and therefore applies in every location.</summary>
        public bool IsGlobal { get; set; }

        /// <summary>The most this player can ever catch here, or -1 when at least one merged entry sets no limit.</summary>
        public int CatchLimit { get; set; } = -1;

        /// <summary>Whether any merged entry marks this as a boss catch, which changes the fishing minigame.</summary>
        public bool IsBossFish { get; set; }

        /// <summary>Whether a training rod works here. <c>null</c> means no merged entry said either way, so the game's default rule applies.</summary>
        public bool? AllowsTrainingRod { get; set; }

        /// <summary>Whether any merged entry carries a condition beyond a season check, so the row needs a footnote rather than being shown as unconditional.</summary>
        public bool HasExtraConditions { get; set; }
    }
}
