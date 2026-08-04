using System.Collections.Generic;

namespace FishingLogbook.Framework.Models.Fish
{
    /// <summary>A single window during which a fish can spawn. The start is inclusive and the end is exclusive.</summary>
    /// <param name="StartTime">The earliest spawn time in 26-hour game format (e.g. 600 for 6:00am).</param>
    /// <param name="EndTime">The latest spawn time in 26-hour game format (e.g. 2600 for 2:00am).</param>
    public record FishTimeWindow(int StartTime, int EndTime);

    /// <summary>The spawn requirements parsed out of one <c>Data/Fish</c> entry.</summary>
    public class ParsedFishData
    {
        /// <summary>The unqualified item ID which keys this entry in <c>Data/Fish</c>.</summary>
        public string ItemId { get; set; }

        /// <summary>The internal (English) fish name from field 0.</summary>
        public string InternalName { get; set; }

        /// <summary>Whether field 1 was <c>trap</c>, meaning this fish is only obtainable from a crab pot.</summary>
        public bool IsTrapFish { get; set; }

        /// <summary>The minimum size in inches used for fishing stats.</summary>
        public int MinSize { get; set; }

        /// <summary>The maximum size in inches used for fishing stats.</summary>
        public int MaxSize { get; set; }

        /// <summary>How often the fish darts during the minigame, between 15 and 100. Field 1.</summary>
        public int DartChance { get; set; }

        /// <summary>How the bobber behaves during the minigame. One of <c>mixed</c>, <c>smooth</c>, <c>floater</c>, <c>sinker</c> or <c>dart</c>. Field 2.</summary>
        public string DartBehavior { get; set; } = "mixed";

        /// <summary>The windows during which the fish can spawn. Field 5.</summary>
        public List<FishTimeWindow> TimeWindows { get; set; } = new();

        /// <summary>The required weather. One of <c>sunny</c>, <c>rainy</c> or <c>both</c>. Field 7.</summary>
        public string Weather { get; set; } = "both";

        /// <summary>The minimum tiles from land the bobber must land for the chance to avoid a penalty. Field 9.</summary>
        public int MaxDepth { get; set; }

        /// <summary>The base catch chance once the fish has been selected as a candidate. Field 10.</summary>
        public float BaseChance { get; set; }

        /// <summary>The multiplier applied when penalising a cast closer to land than <see cref="MaxDepth"/>. Field 11.</summary>
        public float DepthMultiplier { get; set; }

        /// <summary>The minimum fishing level needed for the fish to appear. Field 12.</summary>
        public int MinFishingLevel { get; set; }

        /// <summary>Whether the fish may be picked for the first-catch tutorial. Field 13.</summary>
        public bool CanBeTutorialCatch { get; set; }

        /// <summary>The chance this crab pot fish is picked, assuming no earlier entry was selected. Field 2.</summary>
        public float TrapChance { get; set; }

        /// <summary>The water body type this crab pot fish belongs to, matched against a fish area's <c>CrabPotFishTypes</c>. Field 4.</summary>
        public string TrapWaterType { get; set; } = "freshwater";
    }
}
