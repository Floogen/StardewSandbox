using CropTraits.Framework.Models;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class DilutionHelper
    {
        public const double BASE_DILUTION_LOSS_CHANCE = 0.35;
        public const double LOSS_REDUCTION_PER_QUALITY_STEP = 0.05;

        // Each trait independently risks being lost, and Stable offers no protection here
        public static TraitSet Dilute(TraitSet parentTraits, int parentQuality, Random random)
        {
            TraitSet resultTraits = new TraitSet();
            double lossChance = GetLossChance(parentQuality);

            foreach (KeyValuePair<string, int> trait in parentTraits.Entries)
            {
                if (random.NextDouble() >= lossChance)
                {
                    resultTraits.Set(trait.Key, trait.Value);
                }
            }

            return resultTraits;
        }

        // Higher quality traited produce holds onto its traits better (35% normal, 30% silver, 25% gold, 20% iridium)
        public static double GetLossChance(int parentQuality)
        {
            return BASE_DILUTION_LOSS_CHANCE - (QualityHelper.GetQualityStep(parentQuality) * LOSS_REDUCTION_PER_QUALITY_STEP);
        }
    }
}
