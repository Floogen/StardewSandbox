namespace CropTraits.Framework.Traits
{
    /// <summary>
    /// A trait the Seed Splicer fuses from two other traits, each carried by a different parent.
    /// Combination traits are single tier, never rolled by mutation and replace the traits they're made from.
    /// </summary>
    public abstract class CombinationTrait : Trait
    {
        public const double DEFAULT_FUSION_CHANCE = 0.25;

        /// <summary>
        /// The ID of the first trait fused into this one.
        /// </summary>
        public abstract string FirstTraitId { get; }

        /// <summary>
        /// The ID of the second trait fused into this one.
        /// </summary>
        public abstract string SecondTraitId { get; }

        /// <summary>
        /// The chance the Seed Splicer fuses the pair when each parent carries one of the traits.
        /// </summary>
        public virtual double FusionChance => DEFAULT_FUSION_CHANCE;

        public sealed override int MaxTier => 1;
        public sealed override bool CanBeRolled => false;

        public bool IsComposedOf(string traitId)
        {
            return traitId == FirstTraitId || traitId == SecondTraitId;
        }
    }
}
