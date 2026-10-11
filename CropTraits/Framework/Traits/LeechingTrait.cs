using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace CropTraits.Framework.Traits
{
    // Fused from Depleting and Greedy, a neutral trade that feeds itself at its neighbors' expense
    public class LeechingTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Leeching";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => DepletingTrait.ID;
        public override string SecondTraitId => GreedyTrait.ID;

        /// <summary>
        /// Takes the strongest quality fertilizer around it into its own soil, leaving that soil bare. Only quality fertilizers move, only when they beat its own.
        /// Neighbors are visited in a fixed order, so a tie always goes to the same tile.
        /// </summary>
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.Dirt is null)
            {
                return;
            }

            int ownRank = FertilizerHelper.GetQualityFertilizerRank(context.Dirt.fertilizer.Value);
            if (ownRank == FertilizerHelper.OTHER_FERTILIZER_RANK)
            {
                return;
            }

            HoeDirt bestDirt = null;
            int bestRank = ownRank;
            foreach (Vector2 neighborTile in Utility.getSurroundingTileLocationsArray(context.Tile))
            {
                if (context.Location.terrainFeatures.TryGetValue(neighborTile, out TerrainFeature feature) is false || feature is not HoeDirt neighborDirt)
                {
                    continue;
                }

                int neighborRank = FertilizerHelper.GetQualityFertilizerRank(neighborDirt.fertilizer.Value);
                if (neighborRank > bestRank)
                {
                    bestDirt = neighborDirt;
                    bestRank = neighborRank;
                }
            }

            if (bestDirt is null)
            {
                return;
            }

            context.Dirt.fertilizer.Value = bestDirt.fertilizer.Value;
            bestDirt.fertilizer.Value = null;
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details");
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
