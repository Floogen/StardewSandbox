using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;

namespace CropTraits.Framework.Traits
{
    // Fused from Mimic and Variegated, named after the plant chimeras that variegation comes from
    public class ChimeraTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Chimera";

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override string FirstTraitId => MimicTrait.ID;
        public override string SecondTraitId => VariegatedTrait.ID;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            MimicTrait.CopyTrait(context, true);
        }

        public override Color ModifyTint(Color tint, string seedId, int tier)
        {
            return VariegatedTrait.GetTint(seedId);
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
