using CropTraits.Framework.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class DilutionHelper
    {
        public const double BASE_SPLIT_CHANCE = 0.20;
        public const double SPLIT_CHANCE_PER_QUALITY_STEP = 0.05;

        /// <summary>
        /// Dilution either splits off exactly one trait (picked evenly) or wipes every trait, giving plain seeds.
        /// A split is the rare outcome, so crossbreeding stays the precise way to shed traits and dilution is the gamble.
        /// Stable offers no protection here.
        /// </summary>
        public static TraitSet Dilute(TraitSet parentTraits, int parentQuality, Random random)
        {
            if (parentTraits.Count == 0 || random.NextDouble() >= GetSplitChance(parentQuality))
            {
                return new TraitSet();
            }

            // Ids are in ordinal order, so the pick only depends on the roll
            List<string> traitIds = parentTraits.Ids.ToList();

            TraitSet resultTraits = parentTraits.Clone();
            resultTraits.Remove(traitIds[random.Next(traitIds.Count)]);

            return resultTraits;
        }

        // Higher quality traited produce splits more often (20% normal, 25% silver, 30% gold, 35% iridium)
        public static double GetSplitChance(int parentQuality)
        {
            return BASE_SPLIT_CHANCE + (QualityHelper.GetQualityStep(parentQuality) * SPLIT_CHANCE_PER_QUALITY_STEP);
        }
    }
}
