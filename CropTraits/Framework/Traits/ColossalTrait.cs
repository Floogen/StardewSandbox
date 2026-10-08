using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.GameData.GiantCrops;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Traits
{
    public class ColossalTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Colossal";

        public const double GIANT_CROP_CHANCE_PER_TIER = 0.02;
        public const string GIANT_CROPS_ASSET = "Data/GiantCrops";

        private static readonly string[] CONFLICTS = { SmallTrait.ID };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Read from the asset so giant crops added by other mods count too
        public override bool CanApplyTo(CropData cropData)
        {
            string harvestItemId = ItemRegistry.QualifyItemId(cropData?.HarvestItemId);
            if (string.IsNullOrEmpty(harvestItemId))
            {
                return false;
            }

            return Game1.content.Load<Dictionary<string, GiantCropData>>(GIANT_CROPS_ASSET).Values.Any(giantCrop => ItemRegistry.QualifyItemId(giantCrop.FromItemId) == harvestItemId);
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
