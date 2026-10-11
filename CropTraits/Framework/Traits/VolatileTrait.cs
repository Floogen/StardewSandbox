using CropTraits.Framework.Utilities.Helpers;

namespace CropTraits.Framework.Traits
{
    public class VolatileTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Volatile";

        public const double MUTATION_BONUS_PER_TIER = 0.05;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Rare;

        public override double ModifyMutationChance(double chance, int tier, bool isFirstRoll)
        {
            if (isFirstRoll is false)
            {
                return chance;
            }

            return chance + (MUTATION_BONUS_PER_TIER * tier);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { mutationBonus = ToPercentValue(MUTATION_BONUS_PER_TIER * tier) });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { baseMutationChance = ToPercentValue(MutationHelper.BASE_MUTATION_CHANCE) });
        }
    }
}
