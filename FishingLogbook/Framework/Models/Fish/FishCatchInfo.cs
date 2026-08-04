namespace FishingLogbook.Framework.Models.Fish
{
    /// <summary>A player's catch record for one fish, read from <see cref="StardewValley.Farmer.fishCaught"/>.</summary>
    /// <param name="TimesCaught">How many of this fish the player has caught.</param>
    /// <param name="BiggestCatchInches">The largest recorded catch in inches, or 0 if the game never recorded a size.</param>
    public readonly record struct FishCatchInfo(int TimesCaught, int BiggestCatchInches)
    {
        /// <summary>An empty record, used for fish the player has never caught.</summary>
        public static FishCatchInfo None { get; } = new(0, 0);

        /// <summary>Whether the player has caught this fish at least once.</summary>
        public bool HasCaught => this.TimesCaught > 0;

        /// <summary>Whether a size was recorded. Crab pot catches and fish pond harvests have no size.</summary>
        public bool HasSize => this.BiggestCatchInches > 0;
    }
}
