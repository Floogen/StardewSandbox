using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Utilities.Helpers
{
    // Called every time something checks whether it can walk over soil, so it reads the cached visible traits and leaves early when it can
    public static class PassabilityHelper
    {
        public const int CLIMBING_MIN_PHASE = 1; // A fresh planting never blocks the way, so a field can't wall the player in the day it's planted

        /// <summary>
        /// Returns whether the crop can be walked through, given what the game decided.
        /// Only visible traits count, so a hidden mutation never changes how a crop blocks movement before it's revealed. Junimos always get through, matching how the game treats trellis crops.
        /// </summary>
        public static bool IsPassable(HoeDirt dirt, Character character, bool isPassable)
        {
            Crop crop = dirt.crop;
            if (crop is null || crop.dead.Value || character is JunimoHarvester)
            {
                return isPassable;
            }

            TraitSet traits = CropAppearanceHelper.GetVisibleTraits(dirt);
            if (traits.Count == 0)
            {
                return isPassable;
            }

            if (crop.raisedSeeds.Value)
            {
                return isPassable || traits.Has(TrailingTrait.ID);
            }

            if (traits.Has(ClimbingTrait.ID) && crop.currentPhase.Value >= CLIMBING_MIN_PHASE)
            {
                return false;
            }

            return isPassable;
        }
    }
}
