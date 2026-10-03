using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;

namespace CropTraits.Framework.Traits
{
    public class ProlificTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Prolific";

        public const double EXTRA_HARVEST_CHANCE_PER_TIER = 0.10;

        public override string Id => ID;
        public override int Weight => 8;

        // The extra produce is created inside the harvest context, so it picks up the crop's traits too
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            if (Game1.random.NextDouble() >= EXTRA_HARVEST_CHANCE_PER_TIER * tier)
            {
                return;
            }

            // A copy of the main harvest keeps its quality, color (flowers) and traits, so the extra stacks with it
            Item extraProduce = context.HarvestedItem is not null ? context.HarvestedItem.getOne() : ItemRegistry.Create(context.QualifiedHarvestItemId);
            if (context.JunimoHarvester is not null)
            {
                context.JunimoHarvester.tryToAddItemToHut(extraProduce);
                return;
            }

            Game1.createItemDebris(extraProduce, new Vector2((xTile * Game1.tileSize) + (Game1.tileSize / 2), (yTile * Game1.tileSize) + (Game1.tileSize / 2)), -1, location);
        }
    }
}
