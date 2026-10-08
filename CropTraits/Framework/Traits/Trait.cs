using CropTraits.Framework.Models;
using StardewValley;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public abstract class Trait
    {
        public const int COMMON_WEIGHT = 12;
        public const int UNCOMMON_WEIGHT = 8;
        public const int RARE_WEIGHT = 4;

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
        /// How often mutation rolls this trait compared to the others. Also sets the default weight.
        /// </summary>
        public virtual TraitRarity Rarity => TraitRarity.Common;

        /// <summary>
        /// How likely the trait is to be rolled during a mutation. Defaults to the rarity's weight, so the rarity shown in the Field Notes stays accurate.
        /// </summary>
        public virtual int Weight => GetRarityWeight(Rarity);

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
        /// Adjusts the chance that the crop mutates the night after planting (the nightly Mutagenic Fertilizer rolls are unaffected).
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
        /// Adjusts how large the produce is drawn (1 is normal size). Only applies to produce, never seeds or artisan goods.
        /// </summary>
        public virtual float ModifyProduceScale(float scale, int tier)
        {
            return scale;
        }

        /// <summary>
        /// Adjusts the sell price multiplier for produce and artisan goods carrying this trait.
        /// </summary>
        public virtual double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// What a single tier does, shown on the trait's Field Notes page. An empty string skips the line.
        /// </summary>
        public virtual string GetTierDetails(int tier)
        {
            return string.Empty;
        }

        /// <summary>
        /// Extra rules about the trait shown under its tier details. An empty string skips the line.
        /// </summary>
        public virtual string GetNotes()
        {
            return string.Empty;
        }

        public static int GetRarityWeight(TraitRarity rarity)
        {
            return rarity switch
            {
                TraitRarity.Uncommon => UNCOMMON_WEIGHT,
                TraitRarity.Rare => RARE_WEIGHT,
                _ => COMMON_WEIGHT
            };
        }

        public static string ToPercent(double value)
        {
            return $"{Math.Round(value * 100)}%";
        }
    }
}
