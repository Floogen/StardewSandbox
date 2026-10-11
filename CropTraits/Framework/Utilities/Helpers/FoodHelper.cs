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

        // Only produce has a seed ID, since artisan goods carry price traits like Tangy but aren't the crop itself
        public static bool IsProduce(Item item)
        {
            return item?.modData.ContainsKey(CropTraits.SEED_ID_MOD_DATA_KEY) ?? false;
        }

        // Inedible items (and harmful ones with negative values) are left alone, as are artisan goods
        public static int ApplyRestorationTraits(Item item, int amount)
        {
            if (amount <= 0 || IsProduce(item) is false)
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

        // Builds a buff named after the trait that gives it, so eating more refreshes it instead of stacking
        public static Buff CreateTraitBuff(string traitId, string buffId, BuffEffects effects, int minutes, int iconSheetIndex)
        {
            string traitName = CropTraits.traitManager.GetTraitName(traitId);
            return new Buff(buffId, source: traitId, displaySource: traitName, duration: minutes * Game1.realMilliSecondsPerGameMinute, iconTexture: Game1.buffsIcons, iconSheetIndex: iconSheetIndex, effects: effects, displayName: traitName);
        }

        // Artisan goods are left alone, so eating Tangy jelly gives no speed boost
        public static IEnumerable<Buff> AddTraitBuffs(Item item, IEnumerable<Buff> buffs)
        {
            if (IsProduce(item) is false)
            {
                return buffs;
            }

            TraitSet traits = TraitModDataHelper.GetTraits(item.modData);
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
