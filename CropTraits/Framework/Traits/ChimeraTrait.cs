using CropTraits.Framework.Models;
using Microsoft.Xna.Framework;

namespace CropTraits.Framework.Traits
{
    // Fused from Mimic and Variegated, named after the plant chimeras that variegation comes from
    public class ChimeraTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Chimera";

        // Shared so the tint matches Variegated
        private static readonly VariegatedTrait _variegatedTrait = new VariegatedTrait();

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
            return _variegatedTrait.ModifyTint(tint, seedId, tier);
        }

        public override string GetTierDetails(int tier)
        {
            return "Tinted like Variegated. Once ready to harvest, copies one trait at its own tier from an adjacent crop of a different type";
        }

        public override string GetNotes()
        {
            return "Covers all eight surrounding tiles. Copies once per planting, even alongside Mimic. Never copies combination traits. Only copies traits its own crop could roll. Flowers keep their own petal colors, both on the crop and as produce.";
        }
    }
}
