using CropTraits.Framework.Models;
using StardewValley;
using System;

namespace CropTraits.Framework.Traits
{
    public class InstructiveTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Instructive";

        public const double EXPERIENCE_BONUS_PER_TIER = 0.25;
        public const int MIN_BONUS_EXPERIENCE = 1;

        public override string Id => ID;

        // Only the player harvesting gets the bonus, matching the game where Junimo harvests give no experience
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            if (context.JunimoHarvester is not null || context.HarvestedItem is not StardewValley.Object harvestedObject)
            {
                return;
            }

            int bonusExperience = Math.Max(MIN_BONUS_EXPERIENCE, (int)Math.Round(GetHarvestExperience(harvestedObject.Price) * EXPERIENCE_BONUS_PER_TIER * tier));
            Game1.player.gainExperience(Farmer.farmingSkill, bonusExperience);
        }

        // Matches the game's own harvest experience formula
        public static double GetHarvestExperience(int price)
        {
            return 16.0 * Math.Log((0.018 * price) + 1.0, Math.E);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { experienceBonus = ToPercentValue(EXPERIENCE_BONUS_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
