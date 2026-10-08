using CropTraits.Framework.Utilities.Helpers;

namespace CropTraits.Framework.Traits
{
    // Fused from Volatile and Resprouting, giving up the regrowths for better mutation odds on every roll
    public class TemperamentalTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Temperamental";

        public const double MUTATION_BONUS = 0.20;

        public override string Id => ID;
        public override string FirstTraitId => VolatileTrait.ID;
        public override string SecondTraitId => ResproutingTrait.ID;

        public override double ModifyMutationChance(double chance, int tier, bool isFirstRoll)
        {
            return chance + MUTATION_BONUS;
        }

        public override string GetTierDetails(int tier)
        {
            return $"+{ToPercent(MUTATION_BONUS)} mutation chance on every roll";
        }

        public override string GetNotes()
        {
            return $"Mutation chance starts at {ToPercent(MutationHelper.BASE_MUTATION_CHANCE)}. Applies to the roll the night after planting and to Mutagenic Fertilizer's nightly rolls.";
        }
    }
}
