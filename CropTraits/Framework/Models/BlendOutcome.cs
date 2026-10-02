using StardewValley;

namespace CropTraits.Framework.Models
{
    // UpgradedTraits holds only the traits that climbed a tier, at their new tier
    public record BlendOutcome(Item Seeds, TraitSet Traits, bool WasCrossbreed, TraitSet UpgradedTraits);
}
