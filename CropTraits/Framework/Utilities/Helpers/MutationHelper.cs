using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using Microsoft.Xna.Framework;
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
        public const double REPLACE_CHANCE_UNDER_CAP = 0.25;

        // Neighbors never affect the chance, only which trait is rolled. Traits decide which rolls they affect (Volatile only boosts the first)
        public static double GetChance(TraitSet traits, bool isFirstRoll)
        {
            double chance = BASE_MUTATION_CHANCE;
            foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(traits))
            {
                chance = trait.ModifyMutationChance(chance, tier, isFirstRoll);
            }

            return Math.Clamp(chance, 0, 1);
        }

        // Mutagenic Fertilizer adds a roll every night the crop grows (including when applied after planting) until the crop mutates or matures
        public static bool CanRollNightly(HoeDirt dirt, GameLocation location, Vector2 tile)
        {
            Crop crop = dirt.crop;
            if (crop is null || crop.dead.Value || PlantedTraitHelper.IsMature(crop))
            {
                return false;
            }

            // Traits like Fussy ignore every fertilizer, Mutagenic Fertilizer included
            if (CropTraits.traitManager.IgnoresFertilizer(TraitModDataHelper.GetTraits(dirt.modData)))
            {
                return false;
            }

            if (HasMutagenicFertilizer(dirt) is false || dirt.modData.ContainsKey(MutationManager.HAS_MUTATED_MOD_DATA_KEY))
            {
                return false;
            }

            // Leaving the crop unwatered keeps it from maturing, so dry nights can't be used to buy extra rolls
            return IsGrowingTonight(dirt, location, tile);
        }

        // Read at the end of the day, before the game dries the soil. Traits like Deep Rooted and Paddy count as watered
        public static bool IsGrowingTonight(HoeDirt dirt, GameLocation location, Vector2 tile)
        {
            if (dirt.state.Value == HoeDirt.watered || dirt.crop?.GetData()?.NeedsWatering == false)
            {
                return true;
            }

            return CropTraits.traitManager.CountsAsWatered(TraitModDataHelper.GetTraits(dirt.modData), location, tile);
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

            // A combination trait can share a crop with one of its parts, so the rolled trait may conflict with more than one trait
            List<string> conflictingIds = traits.Ids.Where(traitId => CropTraits.traitManager.HasConflict(rolledTrait, traitId)).ToList();
            if (conflictingIds.Count > 0)
            {
                if (isStable)
                {
                    return;
                }

                foreach (string conflictingId in conflictingIds)
                {
                    traits.Remove(conflictingId);
                }

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
                List<string> traitIds = traits.Ids.ToList();
                traits.Remove(traitIds[random.Next(traitIds.Count)]);
            }

            traits.Set(rolledTrait.Id, 1);
        }
    }
}
