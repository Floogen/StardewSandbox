using System.Collections.Generic;

namespace CropTraits.Framework.Models
{
    // BaseTraits is the result before tier upgrades, Traits adds any rare tier upgrades
    public record CrossbreedResult(TraitSet BaseTraits, TraitSet Traits, List<string> FusedCombinationKeys);
}
