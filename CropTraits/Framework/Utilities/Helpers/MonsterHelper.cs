using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using System;

namespace CropTraits.Framework.Utilities.Helpers
{
    // Shared by Slimy and Roosting, which spawn monsters each morning from fully grown outdoor crops
    public static class MonsterHelper
    {
        public const double SPAWN_CHANCE = 0.10;

        // Mine levels picking each tier's variant (green slime and bat, then frost, then lava and red)
        public static readonly int[] MINE_LEVELS_BY_TIER = { 20, 60, 100 };

        public static bool AreMonsterTraitsEnabled => CropTraits.modConfig?.EnableMonsterTraits ?? true;

        // The chance is rolled first so every crop draws the same number of times whether or not the location is full
        public static bool ShouldSpawn(MorningContext context)
        {
            if (AreMonsterTraitsEnabled is false || context.Location.IsOutdoors is false || PlantedTraitHelper.IsMature(context.Dirt.crop) is false)
            {
                return false;
            }

            return context.Random.NextDouble() < SPAWN_CHANCE && context.MonsterSpawnLimit.TryUse();
        }

        public static int GetTierIndex(int tier)
        {
            return Math.Clamp(tier, 1, MINE_LEVELS_BY_TIER.Length) - 1;
        }

        public static int GetMineLevel(int tier)
        {
            return MINE_LEVELS_BY_TIER[GetTierIndex(tier)];
        }

        public static Vector2 GetSpawnPosition(Vector2 tile)
        {
            return tile * Game1.tileSize;
        }

        // Traits like Variegated give the crop's tint, otherwise colored crops like flowers give their own color (white means no color)
        public static Color GetCropColor(Crop crop)
        {
            if (crop is null)
            {
                return Color.White;
            }

            Color traitTint = CropAppearanceHelper.GetCropTint(crop);
            if (traitTint != Color.White)
            {
                return traitTint;
            }

            return crop.programColored.Value ? crop.tintColor.Value : Color.White;
        }

        public static void Spawn(GameLocation location, Monster monster)
        {
            location.characters.Add(monster);
        }

        public static string GetNotes()
        {
            return $"Only outdoors once fully grown. Each location holds at most {MorningManager.MAX_MONSTERS_PER_LOCATION} monsters, counting any already there. Can be turned off in the config, which also stops it from being rolled.";
        }
    }
}
