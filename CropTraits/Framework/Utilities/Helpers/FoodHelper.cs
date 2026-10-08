using CropTraits.Framework.Models;
using StardewValley;
using StardewValley.Buffs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class FoodHelper
    {
        // Set while the game works out health, since health may be based on the energy value and should only be adjusted once
        private static bool _isCalculatingHealth;

        public static void BeginHealthCalculation()
        {
            _isCalculatingHealth = true;
        }

        public static void EndHealthCalculation()
        {
            _isCalculatingHealth = false;
        }

        public static int ApplyEnergyTraits(Item item, int energy)
        {
            return _isCalculatingHealth ? energy : ApplyRestorationTraits(item, energy);
        }

        // Inedible items (and harmful ones with negative values) are left alone
        public static int ApplyRestorationTraits(Item item, int amount)
        {
            if (amount <= 0 || item is null)
            {
                return amount;
            }

            TraitSet traits = TraitModDataHelper.GetTraits(item.modData);
            if (traits.Count == 0)
            {
                return amount;
            }

            double multiplier = CropTraits.traitManager.GetFoodRestorationMultiplier(traits);
            return multiplier == 1 ? amount : Math.Max((int)Math.Round(amount * multiplier), 0);
        }

        public static IEnumerable<Buff> AddTraitBuffs(Item item, IEnumerable<Buff> buffs)
        {
            TraitSet traits = TraitModDataHelper.GetTraits(item?.modData);
            if (traits.Count == 0)
            {
                return buffs;
            }

            List<Buff> traitBuffs = CropTraits.traitManager.GetFoodBuffs(traits);
            if (traitBuffs.Count == 0)
            {
                return buffs;
            }

            return (buffs ?? Enumerable.Empty<Buff>()).Concat(traitBuffs);
        }
    }
}
