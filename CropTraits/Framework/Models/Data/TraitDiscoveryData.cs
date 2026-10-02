using System.Collections.Generic;

namespace CropTraits.Framework.Models.Data
{
    public class TraitDiscoveryData
    {
        public HashSet<int> TiersSeen { get; set; } = new();
        public string FirstSeedId { get; set; }
        public int FirstDiscoveredDay { get; set; }
    }
}
