using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class MutationHelper
    {
        public const double BASE_MUTATION_CHANCE = 0.05;
        public const double MUTAGENIC_FERTILIZER_BONUS = 0.10;
        public const double REPLACE_CHANCE_UNDER_CAP = 0.25;

        // Neighbors never affect the chance, only which trait is rolled
        public static double GetChance(HoeDirt dirt, TraitSet traits)
        {
            double chance = BASE_MUTATION_CHANCE;

            if (HasMutagenicFertilizer(dirt))
            {
                chance += MUTAGENIC_FERTILIZER_BONUS;
            }

            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(traits))
            {
                chance = trait.ModifyMutationChance(chance, tier);
            }

            return Math.Clamp(chance, 0, 1);
        }

        public static bool HasMutagenicFertilizer(HoeDirt dirt)
        {
            string fertilizerId = dirt.fertilizer.Value;
            if (string.IsNullOrEmpty(fertilizerId))
            {
                return false;
            }

            // Fertilizer may be stored qualified or unqualified
            return ItemRegistry.QualifyItemId(fertilizerId) == CropTraits.MUTAGENIC_FERTILIZER_QUALIFIED_ID;
        }

        // Upgrades, displaces, adds or replaces according to the mutation rules
        public static void Mutate(TraitSet traits, Trait rolledTrait, Random random)
        {
            if (rolledTrait is null || rolledTrait.IsExclusive)
            {
                return;
            }

            // Exclusive traits like Seedless never share a crop, so the roll is consumed
            if (CropTraits.traitManager.HasExclusiveTrait(traits))
            {
                return;
            }

            // Rolling an existing trait upgrades it or consumes the roll at max tier
            int currentTier = traits.GetTier(rolledTrait.Id);
            if (currentTier > 0)
            {
                if (currentTier < rolledTrait.MaxTier)
                {
                    traits.Set(rolledTrait.Id, currentTier + 1);
                }

                return;
            }

            // Traits like Stable block displacement and replacement, while adding and upgrading still work
            bool isStable = CropTraits.traitManager.HasMutationReplacementBlocker(traits);

            string conflictingId = traits.Ids.FirstOrDefault(traitId => CropTraits.traitManager.HasConflict(rolledTrait, traitId));
            if (conflictingId is not null)
            {
                if (isStable)
                {
                    return;
                }

                traits.Remove(conflictingId);
                traits.Set(rolledTrait.Id, 1);
                return;
            }

            bool isAtCap = traits.Count >= TraitManager.MAX_TRAITS;
            if (isStable)
            {
                if (isAtCap is false)
                {
                    traits.Set(rolledTrait.Id, 1);
                }

                return;
            }

            bool shouldReplace = isAtCap || (traits.Count > 0 && random.NextDouble() < REPLACE_CHANCE_UNDER_CAP);
            if (shouldReplace)
            {
                // Ids are already in ordinal order, so the replaced trait is deterministic for a given seed
                List<string> traitIds = traits.Ids.ToList();
                traits.Remove(traitIds[random.Next(traitIds.Count)]);
            }

            traits.Set(rolledTrait.Id, 1);
        }
    }
}
