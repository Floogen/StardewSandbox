namespace CropTraits.Framework.Models
{
    // Resolvers clone Traits before changing anything, so the cached key never goes stale
    public record BlendParent(TraitSet Traits, int Quality)
    {
        public string TraitKey { get; } = Traits.ToKey();
        public string SortKey => $"{TraitKey}@{Quality}";
    }
}
