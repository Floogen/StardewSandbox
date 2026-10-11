using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewValley;

namespace CropTraits.Framework.Traits
{
    // Fused from Slimy and Thorny, dropping Slime instead of spawning slimes
    public class GelatinousTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Gelatinous";

        public const string SLIME_ID = "(O)766";
        public const double DROP_CHANCE = 0.25;

        public override string Id => ID;
        public override string FirstTraitId => SlimyTrait.ID;
        public override string SecondTraitId => ThornyTrait.ID;

        // No monster is involved, so it works indoors and whether or not monster traits are turned on
        public override void OnDayStarted(MorningContext context, int tier)
        {
            if (PlantedTraitHelper.IsMature(context.Dirt.crop) is false || context.Random.NextDouble() >= DROP_CHANCE)
            {
                return;
            }

            Vector2 position = (context.Tile * Game1.tileSize) + new Vector2(Game1.tileSize / 2);
            Game1.createItemDebris(ItemRegistry.Create(SLIME_ID), position, -1, context.Location);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { dropChance = ToPercentValue(DROP_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
