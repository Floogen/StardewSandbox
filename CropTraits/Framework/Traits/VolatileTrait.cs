using CropTraits.Framework.Utilities.Helpers;

namespace CropTraits.Framework.Traits
{
    public class VolatileTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Volatile";

        public const double MUTATION_BONUS_PER_TIER = 0.05;

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Rare;

        public override double ModifyMutationChance(double chance, int tier)
        {
            return chance + (MUTATION_BONUS_PER_TIER * tier);
        }

        public override string GetTierDetails(int tier)
        {
            return $"+{ToPercent(MUTATION_BONUS_PER_TIER * tier)} mutation chance";
        }

        public override string GetNotes()
        {
            return $"Mutation chance starts at {ToPercent(MutationHelper.BASE_MUTATION_CHANCE)}.";
        }
    }
}
