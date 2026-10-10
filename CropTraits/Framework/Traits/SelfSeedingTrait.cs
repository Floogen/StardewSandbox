using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public class SelfSeedingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "SelfSeeding";

        public const string HAS_SPREAD_MOD_DATA_KEY = "PeacefulEnd.CropTraits/HasSelfSeeded"; // Marks HoeDirt whose crop already spread this growth cycle, cleared when it's harvested

        public const double SPREAD_CHANCE_PER_TIER = 0.10;

        // Spreading always fills the tiles around it, which Solitary wants kept sparse
        private static readonly string[] CONFLICTS = { SolitaryTrait.ID };

        public override string Id => ID;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override IReadOnlyList<string> ConflictsWith => CONFLICTS;

        // Runs at the end of the day, so the seedling's first night of growth happens straight away
        public override void OnDayEnding(NightlyContext context, int tier)
        {
            TrySpread(context, SPREAD_CHANCE_PER_TIER * tier, false);
        }

        // Shared with Eager. Both use the same flag, so a crop holding both still spreads at most once per growth cycle
        public static void TrySpread(NightlyContext context, double chance, bool startsHalfwayGrown)
        {
            if (context.Dirt?.crop is null || context.Dirt.readyForHarvest() is false || context.Dirt.modData.ContainsKey(HAS_SPREAD_MOD_DATA_KEY))
            {
                return;
            }

            // Traits like Seedless can't be turned into seeds, so they can't spread either
            if (CropTraits.traitManager.HasPropagationBlockingTrait(context.State.Traits))
            {
                return;
            }

            if (IsInSeason(context) is false || context.Random.NextDouble() >= chance)
            {
                return;
            }

            if (PlantingHelper.TrySpreadCrop(context.Location, context.Tile, context.Dirt, context.Random, startsHalfwayGrown))
            {
                context.Dirt.modData[HAS_SPREAD_MOD_DATA_KEY] = "true";
            }
        }

        // Traits like Hardy keep a crop alive past its season, but it only spreads while in season
        public static bool IsInSeason(NightlyContext context)
        {
            return NightlyGrowthHelper.IsInSeasonIgnoringTraits(context.Dirt.crop, context.Location) || CropTraits.traitManager.GrowsInEverySeason(context.State.Traits);
        }

        // A regrowing crop gets one more chance to spread each time it's ready again
        public override void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {
            context.Soil?.modData.Remove(HAS_SPREAD_MOD_DATA_KEY);
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(SPREAD_CHANCE_PER_TIER * tier)} chance each night it's ready to harvest to plant a seedling in empty tilled soil around it";
        }

        public override string GetNotes()
        {
            return "Spreads at most once each time it's ready to harvest and only while in season. The seedling carries the same traits and skips its first mutation roll. Crops with Seedless never spread.";
        }
    }
}
