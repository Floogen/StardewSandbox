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
    public class NightlyGrowthManager : BaseManager
    {
        public const string OUT_OF_SEASON_NIGHTS_MOD_DATA_KEY = "PeacefulEnd.CropTraits/OutOfSeasonNights"; // Nights an outdoor crop has kept growing past its season, on HoeDirt
        public const int NIGHTLY_RANDOM_SEED = 7919; // Keeps the nightly rolls apart from the mutation roll made on the same tile and day

        // Keyed by the dirt itself, since pot dirt isn't stored by tile
        private readonly Dictionary<HoeDirt, NightlyGrowthPlan> _plans = new(ReferenceEqualityComparer.Instance);

        /// <summary>Plans each night's trait effects at the end of the day, then applies them while the game grows crops overnight.</summary>
        public NightlyGrowthManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        // Called at DayEnding after mutations resolve, so a crop's new traits take effect the same night
        public void PlanAllNightlyEffects()
        {
            _plans.Clear();
            if (Context.IsMainPlayer is false)
            {
                return;
            }

            Utility.ForEachLocation(location =>
            {
                try
                {
                    PlanNightlyEffects(location);
                }
                catch (Exception exception)
                {
                    monitor.Log($"Failed to plan nightly trait effects for {location.NameOrUniqueName}: {exception}", LogLevel.Warn);
                }

                return true;
            });
        }

        public void PlanNightlyEffects(GameLocation location)
        {
            // Snapshot first so the plans never depend on the order crops are visited
            Dictionary<Vector2, TileTraitState> traitSnapshot = DirtHelper.CaptureTraitSnapshot(location);
            if (traitSnapshot.Values.All(state => state.Traits.Count == 0))
            {
                return;
            }

            Dictionary<Vector2, HoeDirt> dirtByTile = new();
            foreach ((Vector2 tile, HoeDirt dirt) in DirtHelper.GetAllDirt(location))
            {
                if (traitSnapshot.ContainsKey(tile))
                {
                    dirtByTile.TryAdd(tile, dirt);
                }
            }

            Dictionary<Vector2, NightlyGrowthPlan> plansByTile = new();
            int locationHash = Game1.hash.GetDeterministicHashCode(location.NameOrUniqueName);
            bool isOutdoors = location.IsOutdoors;
            bool isRaining = location.IsRainingHere();

            // Visited in tile order so each crop's rolls are repeatable for a given day and save
            List<KeyValuePair<Vector2, TileTraitState>> traitedTiles = traitSnapshot
                .Where(entry => entry.Value.Traits.Count > 0)
                .OrderBy(entry => entry.Key.X)
                .ThenBy(entry => entry.Key.Y)
                .ToList();

            foreach ((Vector2 tile, TileTraitState state) in traitedTiles)
            {
                Random random = Utility.CreateDaySaveRandom(tile.X, tile.Y, (double)locationHash + NIGHTLY_RANDOM_SEED);
                NightlyContext context = new NightlyContext(location, tile, dirtByTile.GetValueOrDefault(tile), state, GetOrCreatePlan(plansByTile, tile), random, isOutdoors, isRaining, GetNeighbors(tile, traitSnapshot, plansByTile));

                foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(state.Traits))
                {
                    trait.OnDayEnding(context, tier);
                }
            }

            foreach ((Vector2 tile, NightlyGrowthPlan plan) in plansByTile)
            {
                if (plan.HasEffects && dirtByTile.TryGetValue(tile, out HoeDirt dirt))
                {
                    _plans[dirt] = plan;
                }
            }
        }

        // Neighbors get plans too (even untraited ones) so effects like Greedy can change them
        public List<NightlyNeighbor> GetNeighbors(Vector2 tile, Dictionary<Vector2, TileTraitState> traitSnapshot, Dictionary<Vector2, NightlyGrowthPlan> plansByTile)
        {
            List<NightlyNeighbor> neighbors = new();
            foreach (Vector2 neighborTile in Utility.getSurroundingTileLocationsArray(tile))
            {
                if (traitSnapshot.TryGetValue(neighborTile, out TileTraitState neighborState))
                {
                    neighbors.Add(new NightlyNeighbor(neighborTile, neighborState, GetOrCreatePlan(plansByTile, neighborTile)));
                }
            }

            return neighbors;
        }

        public NightlyGrowthPlan GetOrCreatePlan(Dictionary<Vector2, NightlyGrowthPlan> plansByTile, Vector2 tile)
        {
            if (plansByTile.TryGetValue(tile, out NightlyGrowthPlan plan) is false)
            {
                plan = new NightlyGrowthPlan();
                plansByTile[tile] = plan;
            }

            return plan;
        }

        // Called from the Crop.newDay prefix, before the game decides whether the crop grows
        public CropNightState BeginCropNight(Crop crop, ref int state)
        {
            HoeDirt dirt = NightlyGrowthHelper.GetDirt(crop);
            if (dirt is null)
            {
                return null;
            }

            // Each plan is used once, even if another mod runs the crop's night again
            TraitSet traits = TraitModDataHelper.GetTraits(dirt.modData);
            if (_plans.Remove(dirt, out NightlyGrowthPlan plan) is false && traits.Count == 0)
            {
                return null;
            }

            if (plan is not null && plan.CountsAsWatered)
            {
                state = HoeDirt.watered;
            }

            NightlyGrowthHelper.CountOutOfSeasonNight(crop, dirt, traits);

            // Matches the game's own check, where crops without data need water
            bool isGrowing = state == HoeDirt.watered || crop.GetData()?.NeedsWatering == false;

            return new CropNightState(dirt, traits, plan, isGrowing, dirt.readyForHarvest(), crop.currentPhase.Value, crop.dayOfCurrentPhase.Value);
        }

        // Called from the Crop.newDay postfix, after the game's own growth for the night
        public void EndCropNight(Crop crop, CropNightState nightState)
        {
            // Dead crops were killed by the season, while a crop no longer in its dirt became a giant crop or a wild seed harvest
            if (nightState is null || crop.dead.Value || nightState.Dirt.crop != crop)
            {
                return;
            }

            NightlyGrowthHelper.ApplyPlan(crop, nightState);

            // Left unharvested through the whole day it was ready
            if (nightState.WasReady && nightState.Dirt.readyForHarvest() && CropTraits.traitManager.WithersWhenLeftReady(nightState.Traits))
            {
                crop.Kill();
            }

            crop.updateDrawMath(crop.tilePosition);
        }

        // Traits like Hardy keep an outdoor crop alive for a few nights after its season ends
        public bool ExtendsSeason(Crop crop, GameLocation location)
        {
            if (location is null || location.IsOutdoors is false)
            {
                return false;
            }

            HoeDirt dirt = NightlyGrowthHelper.GetDirt(crop);
            if (dirt is null)
            {
                return false;
            }

            int allowedNights = CropTraits.traitManager.GetOutOfSeasonNights(TraitModDataHelper.GetTraits(dirt.modData));
            return allowedNights > 0 && NightlyGrowthHelper.GetOutOfSeasonNights(dirt) <= allowedNights;
        }

        public void Reset()
        {
            _plans.Clear();
        }
    }
}
