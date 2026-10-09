using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Managers
{
    public class MorningManager : BaseManager
    {
        public const int MORNING_RANDOM_SEED = 6151; // Keeps the morning rolls apart from the nightly and mutation rolls made on the same tile and day
        public const int MAX_MONSTERS_PER_LOCATION = 10; // Monsters already in the location count toward this, so leftovers from earlier days never pile up

        /// <summary>Runs each trait's morning effects (main player only), such as Slimy and Roosting spawning monsters.</summary>
        public MorningManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        // Called at DayStarted, after the game has grown crops overnight
        public void ApplyAllMorningEffects()
        {
            if (Context.IsMainPlayer is false)
            {
                return;
            }

            Utility.ForEachLocation(location =>
            {
                try
                {
                    ApplyMorningEffects(location);
                }
                catch (Exception exception)
                {
                    monitor.Log($"Failed to apply morning trait effects for {location.NameOrUniqueName}: {exception}", LogLevel.Warn);
                }

                return true;
            });
        }

        public void ApplyMorningEffects(GameLocation location)
        {
            // Gathered first since traits may add characters to the location, then visited in tile order so each crop's rolls are repeatable
            List<(Vector2 Tile, HoeDirt Dirt, TraitSet Traits)> traitedDirt = DirtHelper.GetAllDirt(location)
                .Where(entry => entry.Dirt.crop is not null && entry.Dirt.crop.dead.Value is false)
                .Select(entry => (entry.Tile, entry.Dirt, Traits: TraitModDataHelper.GetTraits(entry.Dirt.modData)))
                .Where(entry => entry.Traits.Count > 0)
                .OrderBy(entry => entry.Tile.X)
                .ThenBy(entry => entry.Tile.Y)
                .ToList();

            if (traitedDirt.Count == 0)
            {
                return;
            }

            int locationHash = Game1.hash.GetDeterministicHashCode(location.NameOrUniqueName);
            MonsterSpawnLimit monsterSpawnLimit = new MonsterSpawnLimit(MAX_MONSTERS_PER_LOCATION - location.characters.Count(character => character is Monster));

            foreach ((Vector2 tile, HoeDirt dirt, TraitSet traits) in traitedDirt)
            {
                Random random = Utility.CreateDaySaveRandom(tile.X, tile.Y, (double)locationHash + MORNING_RANDOM_SEED);
                MorningContext context = new MorningContext(location, tile, dirt, traits, random, monsterSpawnLimit);

                foreach ((Trait trait, int tier) in CropTraits.traitManager.Resolve(traits))
                {
                    trait.OnDayStarted(context, tier);
                }
            }
        }
    }
}
