using CropTraits.Framework.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class DilutionHelper
    {
        public const double BASE_SPLIT_CHANCE = 0.20;
        public const double TRAITED_SPLIT_CHANCE_PER_QUALITY_STEP = 0.05;
        public const double PLAIN_SPLIT_CHANCE_PER_QUALITY_STEP = 0.025;

        /// <summary>
        /// Dilution either splits off exactly one trait (picked evenly) or wipes every trait, giving plain seeds.
        /// A split is the rare outcome, so crossbreeding stays the precise way to shed traits and dilution is the gamble.
        /// Stable offers no protection here, while traits like Resilient guarantee the split (the split trait is still picked evenly, Resilient included).
        /// </summary>
        public static TraitSet Dilute(TraitSet parentTraits, int traitedQuality, int plainQuality, Random random)
        {
            if (parentTraits.Count == 0)
            {
                return new TraitSet();
            }

            // Rolled first so the draws stay the same whether or not the split is guaranteed
            bool isSplit = random.NextDouble() < GetSplitChance(traitedQuality, plainQuality) || CropTraits.traitManager.GuaranteesDilutionSplit(parentTraits);
            if (isSplit is false)
            {
                return new TraitSet();
            }

            // Ids are in ordinal order, so the pick only depends on the roll
            List<string> traitIds = parentTraits.Ids.ToList();

            TraitSet resultTraits = parentTraits.Clone();
            resultTraits.Remove(traitIds[random.Next(traitIds.Count)]);

            return resultTraits;
        }

        // Both crops raise the split chance, with the traited crop counting twice as much (20% for two normal crops up to 42.5% for two iridium crops)
        public static double GetSplitChance(int traitedQuality, int plainQuality)
        {
            return BASE_SPLIT_CHANCE + (QualityHelper.GetQualityStep(traitedQuality) * TRAITED_SPLIT_CHANCE_PER_QUALITY_STEP) + (QualityHelper.GetQualityStep(plainQuality) * PLAIN_SPLIT_CHANCE_PER_QUALITY_STEP);
        }
    }
}
