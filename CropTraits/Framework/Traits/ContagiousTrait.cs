using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Traits
{
    public class ContagiousTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Contagious";

        public const string HAS_SPREAD_MOD_DATA_KEY = "PeacefulEnd.CropTraits/HasSpreadTrait"; // Marks HoeDirt whose crop already passed on a trait, cleared when something new is planted

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 1;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            SpreadTraits(context, false);
        }

        /// <summary>
        /// Once ready to harvest, copies one of its traits at tier I onto an adjacent crop of the same type that has room for it. Traits like Pervasive copy onto every such crop instead.
        /// Neighbors are read from the live soil rather than the snapshot, so two Contagious crops never fill the same open slot. Both share the same flag, so a crop holding both spreads once per planting.
        /// </summary>
        public static void SpreadTraits(NightlyContext context, bool spreadsToEveryNeighbor)
        {
            if (context.Dirt?.crop is null || context.Dirt.readyForHarvest() is false || context.Dirt.modData.ContainsKey(HAS_SPREAD_MOD_DATA_KEY))
            {
                return;
            }

            List<string> spreadableIds = context.State.Traits.Ids
                .Where(IsSpreadable)
                .ToList();

            if (spreadableIds.Count == 0)
            {
                return;
            }

            // Neighbors and traits are both in a fixed order, so the pick only depends on the roll
            List<(HoeDirt Dirt, TraitSet Traits, string TraitId)> candidates = new();
            foreach (NightlyNeighbor neighbor in context.Neighbors)
            {
                if (neighbor.State.CropId != context.State.CropId || PlantedTraitHelper.TryGetDirt(context.Location, neighbor.Tile, out HoeDirt neighborDirt) is false || neighborDirt.crop is null || neighborDirt.crop.dead.Value)
                {
                    continue;
                }

                TraitSet neighborTraits = TraitModDataHelper.GetTraits(neighborDirt.modData);
                if (neighborTraits.Count >= TraitManager.MAX_TRAITS || CropTraits.traitManager.HasExclusiveTrait(neighborTraits))
                {
                    continue;
                }

                foreach (string traitId in spreadableIds)
                {
                    if (neighborTraits.Has(traitId) is false && CropTraits.traitManager.HasConflictWithAny(traitId, neighborTraits.Ids) is false)
                    {
                        candidates.Add((neighborDirt, neighborTraits, traitId));
                    }
                }
            }

            // Nothing has room yet, so it tries again the next night it's ready
            if (candidates.Count == 0)
            {
                return;
            }

            if (spreadsToEveryNeighbor is false)
            {
                (HoeDirt targetDirt, TraitSet targetTraits, string spreadTraitId) = candidates[context.Random.Next(candidates.Count)];
                GiveTrait(targetDirt, targetTraits, spreadTraitId);
            }
            else
            {
                // Grouped in the order the neighbors were visited, so each neighbor's pick only depends on the roll
                foreach (IGrouping<HoeDirt, (HoeDirt Dirt, TraitSet Traits, string TraitId)> neighborCandidates in candidates.GroupBy(candidate => candidate.Dirt))
                {
                    List<(HoeDirt Dirt, TraitSet Traits, string TraitId)> options = neighborCandidates.ToList();
                    (HoeDirt targetDirt, TraitSet targetTraits, string spreadTraitId) = options[context.Random.Next(options.Count)];
                    GiveTrait(targetDirt, targetTraits, spreadTraitId);
                }
            }

            context.Dirt.modData[HAS_SPREAD_MOD_DATA_KEY] = "true";
        }

        public static void GiveTrait(HoeDirt targetDirt, TraitSet targetTraits, string traitId)
        {
            targetTraits.Set(traitId, 1);
            TraitModDataHelper.SetTraits(targetDirt.modData, targetTraits);

            // A growing crop rebuilds its phases so growth traits like Swift apply, while a grown one has nothing left to change
            if (PlantedTraitHelper.IsMature(targetDirt.crop) is false)
            {
                GrowthHelper.ReapplyGrowthTraits(targetDirt, targetTraits, GrowthHelper.GetPlanter(targetDirt, Game1.MasterPlayer));
            }
        }

        // Only traits mutation can roll are passed on, so combinations stay something only the Seed Splicer makes
        public static bool IsSpreadable(string traitId)
        {
            if (traitId == ID || CropTraits.traitManager.TryGetTrait(traitId, out Trait trait) is false)
            {
                return false;
            }

            return trait.CanBeRolled && trait.IsExclusive is false && trait.BlocksPropagation is false;
        }

        public override string GetTierDetails(int tier)
        {
            return "Once ready to harvest, copies one of its traits (at tier I) onto an adjacent crop of the same type";
        }

        public override string GetNotes()
        {
            return "Covers all eight surrounding tiles. Passes on one trait per planting, waiting until a neighbor has room. Never passes on itself or combination traits. A growing neighbor shows the new trait once it's fully grown, like a mutation.";
        }
    }
}
