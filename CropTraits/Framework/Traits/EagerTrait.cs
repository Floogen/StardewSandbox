using CropTraits.Framework.Models;
using StardewValley;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    // Fused from Self-Seeding and Swift, spreading like Self-Seeding with seedlings that start halfway grown
    public class EagerTrait : CombinationTrait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Eager";

        public const double SPREAD_CHANCE = 0.20;

        // Spreading always fills the tiles around it, which Solitary wants kept sparse
        private static readonly string[] CONFLICTS = { SolitaryTrait.ID };

        public override string Id => ID;
        public override string FirstTraitId => SelfSeedingTrait.ID;
        public override string SecondTraitId => SwiftTrait.ID;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        public override void OnDayEnding(NightlyContext context, int tier)
        {
            SelfSeedingTrait.TrySpread(context, SPREAD_CHANCE, true);
        }

        // A regrowing crop gets one more chance to spread each time it's ready again
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            context.Soil?.modData.Remove(SelfSeedingTrait.HAS_SPREAD_MOD_DATA_KEY);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(SPREAD_CHANCE)} chance each night it's ready to harvest to plant a seedling in empty tilled soil around it, starting halfway grown";
        }

        public override string GetNotes()
        {
            return "Spreads at most once each time it's ready to harvest and only while in season, even alongside Self-Seeding. The seedling carries the same traits and skips its first mutation roll. Crops with Seedless never spread.";
        }
    }
}
