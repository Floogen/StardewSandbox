using System.Collections.Generic;

namespace CropTraits.Framework.Models
{
    // FixedTraits is the pair's deterministic result (tracked in DiscoveryManager), Traits adds any rare tier upgrades
    public record CrossbreedResult(TraitSet FixedTraits, TraitSet Traits, List<string> FusedCombinationKeys);
}
