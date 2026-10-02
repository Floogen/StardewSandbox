using System.Collections.Generic;

namespace CropTraits.Framework.Models.Data
{
    /// <summary>
    /// Sent from farmhands to the host with only the fields that apply set.
    /// </summary>
    public class DiscoveryReportData
    {
        // Instead of using TraitKey, might be worth adjusting TraitSet to handle JSON?
        public string TraitKey { get; set; }
        public string SeedId { get; set; }
        public string PairKey { get; set; }
        public BlendJournalEntryData JournalEntry { get; set; }
        public List<string> FusedCombinationKeys { get; set; }
    }
}
