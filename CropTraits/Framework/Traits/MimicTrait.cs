using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Traits
{
    // Contagious in reverse, pulling a trait from a different crop type instead of pushing to its own
    public class MimicTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Mimic";

        public const string HAS_COPIED_MOD_DATA_KEY = "PeacefulEnd.CropTraits/HasMimicked"; // Marks HoeDirt whose crop already copied a trait, cleared when something new is planted

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 1;

        /// <summary>
        /// Once ready to harvest, copies one trait at tier I from an adjacent crop of a different type, the only way a trait can move between crop types.
        /// Only a neighbor's visible traits count, so a hidden mutation can't be copied before it's revealed. Its own traits are read live, so a trait gained earlier tonight still counts.
        /// </summary>
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.Dirt?.crop is null || context.Dirt.readyForHarvest() is false || context.Dirt.modData.ContainsKey(HAS_COPIED_MOD_DATA_KEY))
            {
                return;
            }

            TraitSet ownTraits = TraitModDataHelper.GetTraits(context.Dirt.modData);
            if (ownTraits.Count >= TraitManager.MAX_TRAITS || CropTraits.traitManager.HasExclusiveTrait(ownTraits))
            {
                return;
            }

            CropData ownCropData = context.Dirt.crop.GetData();

            // Neighbors and their traits are both in a fixed order. A trait offered by two neighbors is only listed once, so the pick only depends on the roll
            List<string> candidateIds = new();
            foreach (NightlyNeighbor neighbor in context.Neighbors)
            {
                if (neighbor.State.CropId == context.State.CropId || PlantedTraitHelper.TryGetDirt(context.Location, neighbor.Tile, out HoeDirt neighborDirt) is false)
                {
                    continue;
                }

                foreach (string traitId in PlantedTraitHelper.GetVisibleTraits(neighborDirt).Ids)
                {
                    if (candidateIds.Contains(traitId) is false && CanCopy(traitId, ownTraits, ownCropData))
                    {
                        candidateIds.Add(traitId);
                    }
                }
            }

            // Nothing to copy yet, so it tries again the next night it's ready
            if (candidateIds.Count == 0)
            {
                return;
            }

            ownTraits.Set(candidateIds[context.Random.Next(candidateIds.Count)], 1);
            TraitModDataHelper.SetTraits(context.Dirt.modData, ownTraits);
            context.Dirt.modData[HAS_COPIED_MOD_DATA_KEY] = "true";
        }

        // Only traits mutation can roll on this crop are copied, so combinations stay something only the Seed Splicer makes
        public static bool CanCopy(string traitId, TraitSet ownTraits, CropData ownCropData)
        {
            if (traitId == ID || ownTraits.Has(traitId) || CropTraits.traitManager.TryGetTrait(traitId, out Trait trait) is false)
            {
                return false;
            }

            if (trait.CanBeRolled is false || trait.IsExclusive || trait.BlocksPropagation || trait.CanApplyTo(ownCropData) is false)
            {
                return false;
            }

            return CropTraits.traitManager.HasConflictWithAny(traitId, ownTraits.Ids) is false;
        }

        public override string GetTierDetails(int tier)
        {
            return "Once ready to harvest, copies one trait (at tier I) from an adjacent crop of a different type";
        }

        public override string GetNotes()
        {
            return "Covers all eight surrounding tiles. Copies one trait per planting, waiting until a neighbor has one it can take. Never copies itself or combination traits. Only copies traits its own crop could roll.";
        }
    }
}
