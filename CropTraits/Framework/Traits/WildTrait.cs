using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;

namespace CropTraits.Framework.Traits
{
    public class WildTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Wild";

        public const double FORAGE_CHANCE_PER_TIER = 0.10;

        // The common forage found on the ground each season
        public static readonly string[] SPRING_FORAGE_IDS = { "(O)16", "(O)18", "(O)20", "(O)22" };
        public static readonly string[] SUMMER_FORAGE_IDS = { "(O)396", "(O)398", "(O)402" };
        public static readonly string[] FALL_FORAGE_IDS = { "(O)404", "(O)406", "(O)408", "(O)410" };
        public static readonly string[] WINTER_FORAGE_IDS = { "(O)412", "(O)414", "(O)416", "(O)418" };

        public override string Id => ID;

        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            if (Game1.random.NextDouble() >= FORAGE_CHANCE_PER_TIER * tier * HarvestDropHelper.GetHarvestPaceMultiplier(context))
            {
                return;
            }

            string[] forageIds = GetForageIds(location.GetSeason());
            HarvestDropHelper.DropNew(context, forageIds[Game1.random.Next(forageIds.Length)], 1, xTile, yTile, location);
        }

        // Uses the location's own season, so the Greenhouse follows the farm and Ginger Island stays in summer
        public static string[] GetForageIds(Season season)
        {
            return season switch
            {
                Season.Summer => SUMMER_FORAGE_IDS,
                Season.Fall => FALL_FORAGE_IDS,
                Season.Winter => WINTER_FORAGE_IDS,
                _ => SPRING_FORAGE_IDS
            };
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { forageChance = ToPercentValue(FORAGE_CHANCE_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { paceNote = HarvestDropHelper.GetHarvestPaceNote() });
        }
    }
}
