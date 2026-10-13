using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class LightningHelper
    {
        public const double CONDUCTIVE_STRIKE_CHANCE = 0.005; // Rolled for each crop, roughly a 45% chance per crop over a full stormy day
        public const int LIGHTNING_RANDOM_SEED = 6143; // Keeps the conductive strike roll apart from the game's own lightning rolls
        public const int MAX_STRIKE_GROWTH_DAYS = 500; // Guards against a crop that never reports ready

        // True only while the game runs a lightning update, so a tool-less hit on soil can be told apart from other damage like bombs
        public static bool IsLightningUpdateActive { get; set; }

        // Called after each of the game's lightning updates (main player only), giving each conductive crop that isn't ready yet its own chance to be struck
        public static void TryStrikeConductiveCrop(int timeOfDay)
        {
            if (Context.IsMainPlayer is false)
            {
                return;
            }

            Farm farm = Game1.getFarm();
            if (farm is null)
            {
                return;
            }

            Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, timeOfDay, LIGHTNING_RANDOM_SEED);

            // Ordered by tile so each crop's roll only depends on the day and time
            List<(Vector2 Tile, HoeDirt Dirt)> targets = farm.terrainFeatures.Pairs
                .Where(pair => pair.Value is HoeDirt dirt && IsStrikeTarget(dirt, includeReady: false))
                .Select(pair => (Tile: pair.Key, Dirt: (HoeDirt)pair.Value))
                .OrderBy(target => target.Tile.X)
                .ThenBy(target => target.Tile.Y)
                .ToList();

            foreach ((Vector2 tile, HoeDirt targetDirt) in targets)
            {
                if (random.NextDouble() < CONDUCTIVE_STRIKE_CHANCE)
                {
                    Strike(farm, tile, targetDirt);
                }
            }
        }

        public static bool IsStrikeTarget(HoeDirt dirt, bool includeReady)
        {
            if (dirt?.crop is null || dirt.crop.dead.Value || (includeReady is false && dirt.readyForHarvest()))
            {
                return false;
            }

            return CropTraits.traitManager.IsConductive(TraitModDataHelper.GetTraits(dirt.modData));
        }

        // Grows the crop one day at a time (matching the game's own growth step) until it's ready, then draws a bolt on it
        public static void Strike(Farm farm, Vector2 tile, HoeDirt dirt)
        {
            Crop crop = dirt.crop;
            for (int day = 0; day < MAX_STRIKE_GROWTH_DAYS && dirt.readyForHarvest() is false; day++)
            {
                NightlyGrowthHelper.AdvanceGrowth(crop, dirt);
            }

            crop.updateDrawMath(tile);

            if (farm is null)
            {
                return;
            }

            farm.lightningStrikeEvent.Fire(new Farm.LightningStrikeEvent { createBolt = true, smallFlash = true, boltPosition = (tile * Game1.tileSize) + new Vector2(Game1.tileSize / 2, 0) });
        }
    }
}
