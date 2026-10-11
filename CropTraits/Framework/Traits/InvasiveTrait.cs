using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Traits
{
    public class InvasiveTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Invasive";

        public const double SPREAD_CHANCE_PER_TIER = 0.05;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors is false || context.Random.NextDouble() >= SPREAD_CHANCE_PER_TIER * tier)
            {
                return;
            }

            List<Vector2> openTiles = Utility.getSurroundingTileLocationsArray(context.Tile)
                .Where(tile => IsOpenGround(context.Location, tile))
                .ToList();

            if (openTiles.Count == 0)
            {
                return;
            }

            Vector2 weedTile = openTiles[context.Random.Next(openTiles.Count)];
            string weedId = ItemRegistry.QualifyItemId(GameLocation.getWeedForSeason(context.Random, context.Location.GetSeason()));
            if (string.IsNullOrEmpty(weedId))
            {
                return;
            }

            context.Location.objects.Add(weedTile, ItemRegistry.Create<Object>(weedId));
        }

        // Diggable ground with nothing on it, so weeds never land on tilled soil, paths or buildings
        public static bool IsOpenGround(GameLocation location, Vector2 tile)
        {
            if (location.isTileOnMap(tile) is false || location.objects.ContainsKey(tile) || location.terrainFeatures.ContainsKey(tile))
            {
                return false;
            }

            if (location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") is null)
            {
                return false;
            }

            return location.CanItemBePlacedHere(tile);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { spreadChance = ToPercentValue(SPREAD_CHANCE_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
