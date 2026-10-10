using CropTraits.Framework.Utilities.Helpers;
using StardewValley.GameData.Crops;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class ColossalTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Colossal";

        public const double GIANT_CROP_CHANCE_PER_TIER = 0.02;

        private static readonly string[] CONFLICTS = { SmallTrait.ID };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Only crops with a giant form, including giant crops added by other mods
        public override bool CanApplyTo(CropData cropData)
        {
            return GiantCropHelper.CanBecomeGiant(cropData);
        }

        public override double ModifyGiantCropChance(double chance, int tier)
        {
            return chance + (GIANT_CROP_CHANCE_PER_TIER * tier);
        }

        public override string GetTierDetails(int tier)
        {
            return $"+{ToPercent(GIANT_CROP_CHANCE_PER_TIER * tier)} chance each night to become a giant crop";
        }

        public override string GetNotes()
        {
            return "Vanilla giant crops have a 1% chance. Only one crop in the grid needs Colossal (the highest tier counts).";
        }
    }
}
