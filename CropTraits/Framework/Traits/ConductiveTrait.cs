using CropTraits.Framework.Utilities.Helpers;

namespace CropTraits.Framework.Traits
{
    // Handled by LightningHelper, which hooks into the game's lightning on the farm
    public class ConductiveTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Conductive";

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 1;
        public override bool IsConductive => true;

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { strikeChance = (LightningHelper.CONDUCTIVE_STRIKE_CHANCE * 100).ToString("0.#") });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
