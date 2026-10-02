using StardewValley.Characters;

namespace CropTraits.Framework.Models
{
    // Active only while Crop.harvest runs for a traited crop
    public class HarvestContext
    {
        public string TraitKey { get; set; }
        public TraitSet Traits { get; set; }
        public string SeedId { get; set; }
        public string QualifiedHarvestItemId { get; set; }
        public int CreatedItemCount { get; set; }

        // Set when a Junimo is harvesting, so extra produce goes to its hut like the main harvest
        public JunimoHarvester JunimoHarvester { get; set; }
    }
}
