using CropTraits.Framework.Models;
using StardewValley;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public abstract class Trait
    {
        /// <summary>
        /// The ID stored in the trait key.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// If this trait is harmful / useless.
        /// </summary>
        public virtual bool IsNegative => false;

        /// <summary>
        /// The max tier possible with 10 being the upper most tier.
        /// </summary>
        public virtual int MaxTier => 3;

        /// <summary>
        /// How likely the trait is to be rolled during a mutation.
        /// </summary>
        public virtual int Weight => 1;

        /// <summary>
        /// The traits this trait cannot coexist with. Conflicts are declared one way and checked in both directions.
        /// </summary>
        public virtual IReadOnlyList<string> ConflictsWith => Array.Empty<string>();

        /// <summary>
        /// If this trait cannot coexist with any other trait.
        /// </summary>
        public virtual bool IsExclusive => false;

        /// <summary>
        /// Whether this trait can be rolled by mutation (occurs the night after planting).
        /// </summary>
        public virtual bool CanBeRolled => true;

        /// <summary>
        /// If this is rejected by the Seed Maker and Seed Splicer.
        /// </summary>
        public virtual bool BlocksPropagation => false;

        /// <summary>
        /// If this trait modifies the produce's price, which also carries it into artisan goods made from the produce.
        /// </summary>
        public virtual bool IsPriceTrait => false;

        /// <summary>
        /// If this trait stops mutations from replacing or displacing the crop's existing traits.
        /// </summary>
        public virtual bool BlocksMutationReplacement => false;

        /// <summary>
        /// Adjusts the growth phase lengths. The last entry is the final phase and must be left alone.
        /// Called after planting, whenever the phases are rebuilt from crop data and when a seed's tooltip estimates its growth time, so it should only change the list it's given.
        /// </summary>
        public virtual void ModifyGrowth(IList<int> phaseDays, CropData cropData, int tier)
        {

        }

        /// <summary>
        /// Adjusts the chance that the crop mutates the night after planting.
        /// </summary>
        public virtual double ModifyMutationChance(double chance, int tier)
        {
            return chance;
        }

        /// <summary>
        /// Adjusts the fertilizer quality level vanilla uses for the crop's harvest quality odds (Deluxe Fertilizer is level 3).
        /// </summary>
        public virtual int ModifyQualityBoostLevel(int level, int tier)
        {
            return level;
        }

        /// <summary>
        /// Called once the harvest finishes, while the harvest context is still active so any items created here also receive traits.
        /// </summary>
        public virtual void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {

        }

        /// <summary>
        /// Adjusts the sell price multiplier for produce and artisan goods carrying this trait.
        /// </summary>
        public virtual double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier;
        }
    }
}
