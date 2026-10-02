namespace CropTraits.Framework.Models.Data
{
    public class BlendJournalEntryData
    {
        public string SeedId { get; set; }

        // Parent sort keys in order, so quality is included
        public string FirstParentKey { get; set; }
        public string SecondParentKey { get; set; }

        public string ResultTraitKey { get; set; }
    }
}
