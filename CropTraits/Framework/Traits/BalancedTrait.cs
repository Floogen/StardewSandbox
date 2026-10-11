using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Swift and Sluggish, trading every growth boost for a pace nothing can change
    public class BalancedTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Balanced";

        // Swift and Sluggish are included since a crop can regain the traits it was fused from, while Thirsty would set its growth back. Traits that speed up or slow down by where the crop grows are included too, as is Conductive since lightning fully grows it
        private static readonly string[] CONFLICTS = { SwiftTrait.ID, SluggishTrait.ID, RainLovingTrait.ID, SunLovingTrait.ID, HothouseTrait.ID, ThirstyTrait.ID, LateBloomerTrait.ID, ParasiticTrait.ID, WatersideTrait.ID, DrylandTrait.ID, PaddyTrait.ID, SolitaryTrait.ID, CompanionTrait.ID, AmphibiousTrait.ID, TropicalTrait.ID, StormbornTrait.ID, DwarfTrait.ID, ConductiveTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => SwiftTrait.ID;
        public override string SecondTraitId => SluggishTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool IgnoresGrowthModifiers => true;

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
