using CropTraits.Framework.Models;
using StardewValley.GameData.Crops;

namespace CropTraits.Framework.Traits
{
    public class DroughtTolerantTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "DroughtTolerant";

        public const double WATERED_CHANCE_PER_TIER = 0.25;

        public override string Id => ID;

        public override bool CanApplyTo(CropData cropData)
        {
            return cropData?.NeedsWatering ?? false;
        }

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.Random.NextDouble() < WATERED_CHANCE_PER_TIER * tier)
            {
                context.Plan.CountsAsWatered = true;
            }
        }

        public override string GetTierDetails(int tier)
        {
            return $"{ToPercent(WATERED_CHANCE_PER_TIER * tier)} chance an unwatered day still counts as watered";
        }

        public override string GetNotes()
        {
            return "The soil still looks dry when this happens.";
        }
    }
}
