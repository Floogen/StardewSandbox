using System.Collections.Generic;

namespace FishingLogbook.Framework.Models.Fish
{
    /// <summary>
    /// The record of how one fish's detail pages were paginated, so a later build can follow the same split without measuring again.
    /// </summary>
    public class DetailPagePlan
    {
        /// <summary>A value identifying the text the plan was measured against, so a plan is discarded once that text changes.</summary>
        public int Signature { get; set; }

        /// <summary>Every answer Parchment gave to an overflow check, in the order they were asked.</summary>
        public List<bool> Answers { get; } = new List<bool>();
    }
}
