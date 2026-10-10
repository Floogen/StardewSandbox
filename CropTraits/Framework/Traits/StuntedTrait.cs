using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // The opposite of Colossal, handled by GiantCropHelper before the game's own roll
    public class StuntedTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Stunted";

        // Titanic is fused from Colossal and only matters on giant crops
        private static readonly string[] CONFLICTS = { ColossalTrait.ID, TitanicTrait.ID };

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override int MaxTier => 1;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;
        public override bool PreventsGiantCrop => true;

        // Only crops with a giant form have anything to lose
        public override bool CanApplyTo(CropData cropData)
        {
            return GiantCropHelper.CanBecomeGiant(cropData);
        }

        public override string GetTierDetails(int tier)
        {
            return "Can never become a giant crop";
        }

        public override string GetNotes()
        {
            return "One Stunted crop anywhere in the grid is enough to stop the whole giant crop. Only appears on crops with a giant form.";
        }
    }
}
