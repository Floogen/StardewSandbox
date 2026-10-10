using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;

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

        // The first produce created (the main harvest), copied for extra harvests so quality and color carry over
        public Item HarvestedItem { get; set; }

        // The soil the crop grows in, so traits like Fragile can read what it has tracked
        public HoeDirt Soil { get; set; }

        // Set when a Junimo is harvesting, so extra produce goes to its hut like the main harvest
        public JunimoHarvester JunimoHarvester { get; set; }

        // Set once the main harvest has been offered to traits like Gilded, so extra produce is never taken in its place
        public bool IsMainHarvestHandled { get; set; }

        // Set when an outdoor crop is harvested past its season (kept alive by traits like Hardy), so traits like Enduring can reward it
        public bool IsOutOfSeason { get; set; }
    }
}
