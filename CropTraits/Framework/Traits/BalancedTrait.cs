using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Swift and Sluggish, trading every growth boost for a pace nothing can change
    public class BalancedTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Balanced";

        private static readonly string[] CONFLICTS = { RainLovingTrait.ID, LateBloomerTrait.ID, ParasiticTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => SwiftTrait.ID;
        public override string SecondTraitId => SluggishTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool IgnoresGrowthModifiers => true;

        public override string GetTierDetails(int tier)
        {
            return "Ignores every change to its growth speed";
        }

        public override string GetNotes()
        {
            return "Speed-Gro, the Agriculturist profession, paddy water and neighboring crops (such as Greedy) never change how fast it grows.";
        }
    }
}
