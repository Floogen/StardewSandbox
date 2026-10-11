using CropTraits.Framework.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Traits
{
    public class FadingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Fading";

        public const double FADE_CHANCE = 0.25;

        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Negative;
        public override TraitRarity Rarity => TraitRarity.Rare;
        public override int MaxTier => 1;

        // Any trait can fade, Fading included. Ids are in ordinal order, so the pick only depends on the roll
        public override void ModifyMadeSeedTraits(TraitSet seedTraits, Random random, int tier)
        {
            if (seedTraits.Count == 0 || random.NextDouble() >= FADE_CHANCE)
            {
                return;
            }

            List<string> traitIds = seedTraits.Ids.ToList();
            string fadingId = traitIds[random.Next(traitIds.Count)];
            int fadedTier = seedTraits.GetTier(fadingId) - 1;
            if (fadedTier <= 0)
            {
                seedTraits.Remove(fadingId);
                return;
            }

            seedTraits.Set(fadingId, fadedTier);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText("details", new { fadeChance = ToPercentValue(FADE_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes");
        }
    }
}
