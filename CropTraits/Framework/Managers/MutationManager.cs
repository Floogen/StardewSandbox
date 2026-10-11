using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Managers
{
    public class MutationManager : BaseManager
    {
        public const string PENDING_MUTATION_MOD_DATA_KEY = "PeacefulEnd.CropTraits/PendingMutation"; // Marks HoeDirt awaiting its first overnight mutation roll
        public const string HAS_MUTATED_MOD_DATA_KEY = "PeacefulEnd.CropTraits/HasMutated"; // Marks HoeDirt whose crop passed a mutation roll, which ends its nightly Mutagenic Fertilizer rolls

        /// <summary>Resolves the mutation roll the night after a crop is planted, plus a roll on each night a crop with Mutagenic Fertilizer grows until it mutates or matures.</summary>
        public MutationManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        // Called at DayEnding, before the save, so each result is persisted immediately
        public void ResolveAllMutationRolls()
        {
            if (Context.IsMainPlayer is false)
            {
                return;
            }

            Utility.ForEachLocation(location =>
            {
                ResolveMutationRolls(location);
                return true;
            });
        }

        public void ResolveMutationRolls(GameLocation location)
        {
            List<(Vector2 Tile, HoeDirt Dirt, bool IsFirstRoll)> rollingDirt = DirtHelper.GetAllDirt(location)
                .Where(entry => entry.Dirt.crop is not null)
                .Select(entry => (entry.Tile, entry.Dirt, IsFirstRoll: entry.Dirt.modData.ContainsKey(PENDING_MUTATION_MOD_DATA_KEY)))
                .Where(entry => entry.IsFirstRoll || MutationHelper.CanRollNightly(entry.Dirt, location, entry.Tile))
                .ToList();

            if (rollingDirt.Count == 0)
            {
                return;
            }

            // Snapshot traits first so same-night mutations don't influence each other
            Dictionary<Vector2, TileTraitState> traitSnapshot = DirtHelper.CaptureTraitSnapshot(location);

            // Location hash keeps identical tiles in different locations from sharing a roll
            int locationHash = Game1.hash.GetDeterministicHashCode(location.NameOrUniqueName);

            foreach ((Vector2 tile, HoeDirt dirt, bool isFirstRoll) in rollingDirt)
            {
                dirt.modData.Remove(PENDING_MUTATION_MOD_DATA_KEY);

                try
                {
                    bool hasMutated = TryMutate(tile, dirt, traitSnapshot, locationHash, isFirstRoll, out TraitSet finalTraits);
                    if (hasMutated)
                    {
                        dirt.modData[HAS_MUTATED_MOD_DATA_KEY] = "true";
                    }

                    // The first roll rebuilds every traited crop so fertilizer applied after planting stacks the same as fertilizer applied before, while nightly rolls only rebuild after a mutation
                    if (finalTraits.Count > 0 && (isFirstRoll || hasMutated))
                    {
                        GrowthHelper.ReapplyGrowthTraits(dirt, finalTraits, GrowthHelper.GetPlanter(dirt, Game1.MasterPlayer));
                    }
                }
                catch (Exception exception)
                {
                    monitor.Log($"Failed to resolve mutation at {location.NameOrUniqueName} {tile}: {exception}", LogLevel.Warn);
                }
            }
        }

        // Returns true when the roll succeeds, even if the rolled trait changed nothing (such as one already at max tier)
        public bool TryMutate(Vector2 tile, HoeDirt dirt, Dictionary<Vector2, TileTraitState> traitSnapshot, int locationHash, bool isFirstRoll, out TraitSet traits)
        {
            // Copied so the shared snapshot is never altered for neighbors resolved later
            traits = traitSnapshot.TryGetValue(tile, out TileTraitState state) ? state.Traits.Clone() : new TraitSet();

            Random random = Utility.CreateDaySaveRandom(tile.X, tile.Y, locationHash);
            if (random.NextDouble() >= MutationHelper.GetChance(traits, isFirstRoll))
            {
                return false;
            }

            Trait rolledTrait = CropTraits.traitManager.RollTrait(random, NeighborInfluenceHelper.GetWeightBonuses(tile, traitSnapshot), dirt.crop.GetData());
            if (rolledTrait is null)
            {
                return true;
            }

            string originalKey = traits.ToKey();
            MutationHelper.Mutate(traits, rolledTrait, random);

            if (traits.ToKey() != originalKey)
            {
                TraitModDataHelper.SetTraits(dirt.modData, traits);
            }

            return true;
        }
    }
}
