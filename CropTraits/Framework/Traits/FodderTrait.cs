using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;

namespace CropTraits.Framework.Traits
{
    public class FodderTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Fodder";

        public const string HAY_ID = "(O)178";
        public const double HAY_CHANCE_PER_TIER = 0.10;

        public override string Id => ID;

        // Dropped as an item like the hay from wheat, so it can be put in a hay hopper
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            if (Game1.random.NextDouble() < HAY_CHANCE_PER_TIER * tier * HarvestDropHelper.GetHarvestPaceMultiplier(context))
            {
                HarvestDropHelper.DropNew(context, HAY_ID, 1, xTile, yTile, location);
            }
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { hayChance = ToPercentValue(HAY_CHANCE_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { paceNote = HarvestDropHelper.GetHarvestPaceNote() });
        }
    }
}
