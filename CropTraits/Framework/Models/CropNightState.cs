using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Models
{
    // Captured before the game grows a traited (or planned) crop overnight, so the changes can be applied or undone afterward
    public record CropNightState(HoeDirt Dirt, TraitSet Traits, NightlyGrowthPlan Plan, bool IsGrowing, bool WasReady, int CurrentPhase, int DayOfCurrentPhase);
}
