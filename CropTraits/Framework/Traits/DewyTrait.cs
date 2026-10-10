using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Traits
{
    public class DewyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Dewy";

        // Tier I covers the four tiles beside it, tier II adds the corners
        public static readonly Vector2[] SIDE_OFFSETS = { new Vector2(0, -1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0) };
        public static readonly Vector2[] CORNER_OFFSETS = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 2;

        // Only ground soil is watered, matching how sprinklers skip garden pots
        public override void OnDayStarted(MorningContext context, int tier)
        {
            WaterTiles(context, SIDE_OFFSETS);
            if (tier >= 2)
            {
                WaterTiles(context, CORNER_OFFSETS);
            }
        }

        public static void WaterTiles(MorningContext context, Vector2[] offsets)
        {
            foreach (Vector2 offset in offsets)
            {
                if (context.Location.terrainFeatures.TryGetValue(context.Tile + offset, out TerrainFeature feature) && feature is HoeDirt dirt && dirt.state.Value != HoeDirt.watered)
                {
                    dirt.state.Value = HoeDirt.watered;
                }
            }
        }

        public override string GetTierDetails(int tier)
        {
            return tier == 1 ? "Waters the soil on its four sides each morning" : "Waters all eight soil tiles around it each morning";
        }

        public override string GetNotes()
        {
            return "Doesn't water its own soil or garden pots.";
        }
    }
}
