using System.Collections.Generic;

namespace CropTraits.Framework.Models.Data
{
    public class CropDiscoveryData
    {
        // Trait ID to every tier seen on this crop
        public Dictionary<string, HashSet<int>> TraitTiers { get; set; } = new();
    }
}
