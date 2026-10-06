using System.Collections.Generic;

namespace CropTraits.Framework.Models.Data
{
    // Saved by the host through SMAPI save data
    public class DiscoveryData
    {
        public Dictionary<string, TraitDiscoveryData> Traits { get; set; } = new();
        public HashSet<string> Combinations { get; set; } = new();

        // Keyed by the pair key used for blend seeding
        public Dictionary<string, BlendJournalEntryData> BlendJournal { get; set; } = new();

        // Keyed by seed ID, since crops are identified by their seed everywhere else
        public Dictionary<string, CropDiscoveryData> Crops { get; set; } = new();
    }
}
