using System.Collections.Generic;

namespace CropTraits.Framework.Models.Data
{
    public class BlendJournalEntryData
    {
        public string SeedId { get; set; }

        // Parent sort keys in order, so quality is included
        public string FirstParentKey { get; set; }
        public string SecondParentKey { get; set; }

        // Every distinct result seen for the pair, in the order they were first seen
        public List<string> ResultTraitKeys { get; set; } = new();

        // Single result from before results varied per splice, moved into ResultTraitKeys on load
        public string ResultTraitKey { get; set; }

        // The day the pair was first crossbred, kept when the pair is crossbred again. Entries from before this was added have 0
        public int FirstSplicedDay { get; set; }
    }
}
