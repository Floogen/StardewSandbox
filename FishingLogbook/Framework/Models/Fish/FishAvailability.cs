using System.Collections.Generic;
using System.Linq;

namespace FishingLogbook.Framework.Models.Fish
{
    /// <summary>A page-ready view of when and where a fish can be caught.</summary>
    public class FishAvailability
    {
        /// <summary>The spawn windows formatted for display, or <c>null</c> when the fish is available all day.</summary>
        public string TimeSummary { get; set; }

        /// <summary>The weather restriction formatted for display, or <c>null</c> when weather does not matter.</summary>
        public string WeatherSummary { get; set; }

        /// <summary>The merged location rows, ordered with the player's current location first.</summary>
        public List<FishAvailabilityRow> Rows { get; set; } = new();

        /// <summary>The lowest fishing level at which the fish becomes catchable anywhere.</summary>
        public int MinFishingLevel => this.Rows.Count > 0 ? this.Rows.Min(row => row.MinFishingLevel) : 0;

        /// <summary>Whether the fish is reachable somewhere without Magic Bait.</summary>
        public bool IsReachableWithoutMagicBait => this.Rows.Any(row => !row.RequiresMagicBait);

        /// <summary>Whether any row needs a footnote about conditions the summary could not express.</summary>
        public bool NeedsConditionFootnote => this.Rows.Any(row => row.HasExtraConditions);

        /// <summary>Whether Magic Bait gates every route to this fish, rather than only some of them.</summary>
        public bool RequiresMagicBaitEverywhere => this.Rows.Count > 0 && this.IsReachableWithoutMagicBait is false;

        /// <summary>Whether this fish can turn up as a boss catch anywhere.</summary>
        public bool IsBossFish => this.Rows.Any(row => row.IsBossFish);

        /// <summary>Whether the fishing level requirement is the same everywhere, so it can be stated once.</summary>
        public bool HasUniformFishingLevel => this.Rows.Count > 0 && this.Rows.All(row => row.MinFishingLevel == this.Rows[0].MinFishingLevel);

        /// <summary>The most this player can ever catch, or -1 when at least one spot sets no limit. A limit only holds for the fish as a whole when every route is capped.</summary>
        public int CatchLimit => this.Rows.Count > 0 && this.Rows.All(row => row.CatchLimit > 0) ? this.Rows.Max(row => row.CatchLimit) : -1;

        /// <summary>Whether no route to this fish accepts a training rod, taking the game's default difficulty rule into account.</summary>
        public bool BlocksTrainingRod { get; set; }

        /// <summary>Whether anything here is worth its own section, so an ordinary fish can skip the heading entirely.</summary>
        public bool HasRequirements => this.MinFishingLevel > 0 || this.RequiresMagicBaitEverywhere || this.IsBossFish || this.CatchLimit > 0;
    }
}
