namespace CropTraits.Framework.Models
{
    // What a crop's traits (and its neighbors' traits) want to happen to it tonight, applied while the game grows the crop
    public class NightlyGrowthPlan
    {
        // Grows as if watered, without changing how the soil looks
        public bool CountsAsWatered { get; set; }

        // Undoes the growth the game applies tonight, if the crop grows at all
        public bool SkipsGrowth { get; set; }

        // Extra days of growth on top of the game's, only on nights the crop grows
        public int ExtraGrowthDays { get; set; }

        // Steps back a day of growth on nights the crop doesn't grow
        public bool LosesGrowthWhenDry { get; set; }

        // Set once a Nurturing neighbor has given this crop an extra day tonight, so several can't stack
        public bool IsNurtured { get; set; }

        // Kills the crop overnight, whether or not it grows
        public bool IsFlattened { get; set; }

        public bool HasEffects => CountsAsWatered || SkipsGrowth || ExtraGrowthDays > 0 || LosesGrowthWhenDry || IsFlattened;
    }
}
