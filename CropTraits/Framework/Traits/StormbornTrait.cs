using CropTraits.Framework.Models;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Tender and Conductive, feeding on the storms that would have flattened it
    public class StormbornTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Stormborn";

        // Tender would flatten it in the very storms it thrives in
        private static readonly string[] CONFLICTS = { TenderTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => TenderTrait.ID;
        public override string SecondTraitId => ConductiveTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        // Keeps Conductive's lightning protection, since it was fused from it
        public override bool IsConductive => true;

        // Uses the weather of the day that's ending, like Tender
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            if (context.IsOutdoors && context.Location.IsLightningHere())
            {
                context.Plan.ExtraGrowthDays++;
            }
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
