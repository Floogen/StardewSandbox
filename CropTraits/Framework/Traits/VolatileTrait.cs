namespace CropTraits.Framework.Traits
{
    public class VolatileTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Volatile";

        public const double MUTATION_BONUS_PER_TIER = 0.05;

        public override string Id => ID;
        public override int Weight => 4;

        public override double ModifyMutationChance(double chance, int tier)
        {
            return chance + (MUTATION_BONUS_PER_TIER * tier);
        }
    }
}
