namespace CropTraits.Framework.Models
{
    // Resolvers clone Traits before changing anything, so the cached key never goes stale
    public record BlendParent(TraitSet Traits, int Quality)
    {
        public string TraitKey { get; } = Traits.ToKey();
        public const char QUALITY_SEPARATOR = '@';

        public string SortKey => $"{TraitKey}{QUALITY_SEPARATOR}{Quality}";

        public static bool TryParseSortKey(string sortKey, out TraitSet traits, out int quality)
        {
            traits = new TraitSet();
            quality = 0;

            int separatorIndex = string.IsNullOrEmpty(sortKey) ? -1 : sortKey.LastIndexOf(QUALITY_SEPARATOR);
            if (separatorIndex < 0 || int.TryParse(sortKey[(separatorIndex + 1)..], out quality) is false)
            {
                return false;
            }

            traits = TraitSet.FromKey(sortKey[..separatorIndex]);
            return true;
        }
    }
}
