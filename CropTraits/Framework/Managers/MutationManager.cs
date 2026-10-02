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
        public const string PENDING_MUTATION_MOD_DATA_KEY = "PeacefulEnd.CropTraits/PendingMutation"; // Marks HoeDirt awaiting its overnight mutation roll

        /// <summary>Resolves the once per lifetime mutation roll the night after a crop is planted.</summary>
        public MutationManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        // Called at DayEnding, before the save, so each result is persisted immediately
        public void ResolveAllPendingMutations()
        {
            if (Context.IsMainPlayer is false)
            {
                return;
            }

            Utility.ForEachLocation(location =>
            {
                ResolvePendingMutations(location);
                return true;
            });
        }

        public void ResolvePendingMutations(GameLocation location)
        {
            List<(Vector2 Tile, HoeDirt Dirt)> pendingDirt = DirtHelper.GetAllDirt(location)
                .Where(entry => entry.Dirt.crop is not null && entry.Dirt.modData.ContainsKey(PENDING_MUTATION_MOD_DATA_KEY))
                .ToList();

            if (pendingDirt.Count == 0)
            {
                return;
            }

            // Snapshot traits first so same-night mutations don't influence each other
            Dictionary<Vector2, TileTraitState> traitSnapshot = DirtHelper.CaptureTraitSnapshot(location);

            // Location hash keeps identical tiles in different locations from sharing a roll
            int locationHash = Game1.hash.GetDeterministicHashCode(location.NameOrUniqueName);

            foreach ((Vector2 tile, HoeDirt dirt) in pendingDirt)
            {
                dirt.modData.Remove(PENDING_MUTATION_MOD_DATA_KEY);

                try
                {
                    TraitSet finalTraits = TryMutate(tile, dirt, traitSnapshot, locationHash);

                    // Rebuilt for every traited crop so fertilizer applied after planting stacks the same as fertilizer applied before
                    if (finalTraits.Count > 0)
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

        // Returns the crop's traits after the roll, whether or not it mutated
        public TraitSet TryMutate(Vector2 tile, HoeDirt dirt, Dictionary<Vector2, TileTraitState> traitSnapshot, int locationHash)
        {
            // Copied so the shared snapshot is never altered for neighbors resolved later
            TraitSet traits = traitSnapshot.TryGetValue(tile, out TileTraitState state) ? state.Traits.Clone() : new TraitSet();

            Random random = Utility.CreateDaySaveRandom(tile.X, tile.Y, locationHash);
            if (random.NextDouble() >= MutationHelper.GetChance(dirt, traits))
            {
                return traits;
            }

            Trait rolledTrait = CropTraits.traitManager.RollTrait(random, NeighborInfluenceHelper.GetWeightBonuses(tile, traitSnapshot));
            if (rolledTrait is null)
            {
                return traits;
            }

            string originalKey = traits.ToKey();
            MutationHelper.Mutate(traits, rolledTrait, random);

            if (traits.ToKey() != originalKey)
            {
                TraitModDataHelper.SetTraits(dirt.modData, traits);
            }

            return traits;
        }
    }
}
