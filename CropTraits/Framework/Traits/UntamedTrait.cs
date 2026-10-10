using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;

namespace CropTraits.Framework.Traits
{
    // Fused from Instructive and Wild, still half a forage plant
    public class UntamedTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Untamed";

        public const int FORAGING_EXPERIENCE = 7; // Matches picking up a piece of forage

        public override string Id => ID;
        public override string FirstTraitId => InstructiveTrait.ID;
        public override string SecondTraitId => WildTrait.ID;

        // Junimos harvest without the player, so they bring the forage home but nobody learns anything
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            if (Game1.random.NextDouble() >= HarvestDropHelper.GetHarvestPaceMultiplier(context))
            {
                return;
            }

            string[] forageIds = WildTrait.GetForageIds(location.GetSeason());
            HarvestDropHelper.DropNew(context, forageIds[Game1.random.Next(forageIds.Length)], 1, xTile, yTile, location);

            if (context.JunimoHarvester is null)
            {
                Game1.player.gainExperience(Farmer.foragingSkill, FORAGING_EXPERIENCE);
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"Harvests find a piece of seasonal forage, giving {FORAGING_EXPERIENCE} Foraging experience";
        }

        public override string GetNotes()
        {
            return $"Always finds forage on crops that take {HarvestDropHelper.FULL_CHANCE_DAYS_PER_HARVEST} or more days per harvest. {HarvestDropHelper.GetHarvestPaceNote()} The forage matches the season where the crop grows. Junimo harvests still find forage but give no experience.";
        }
    }
}
