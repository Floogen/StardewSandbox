using Microsoft.Xna.Framework;
using StardewValley;

namespace CropTraits.Framework.Traits
{
    public class VariegatedTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Variegated";

        // Soft colors, since the tint multiplies the sprite's own colors
        public static readonly Color[] TINTS = { new Color(255, 200, 220), new Color(200, 225, 255), new Color(215, 255, 200), new Color(255, 240, 180), new Color(230, 210, 255), new Color(255, 215, 180) };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override int MaxTier => 1;
        public override TraitRarity Rarity => TraitRarity.Rare;

        // Picked by crop type rather than at random, so Variegated produce of the same crop still stacks
        public override Color ModifyTint(Color tint, string seedId, int tier)
        {
            int hash = Game1.hash.GetDeterministicHashCode(seedId ?? ID) & int.MaxValue;
            return TINTS[hash % TINTS.Length];
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
