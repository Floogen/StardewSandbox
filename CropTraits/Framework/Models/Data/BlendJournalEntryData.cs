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

        // The day the pair was first crossbred, kept when the pair is crossbred again
        public int FirstSplicedDay { get; set; }
    }
}
