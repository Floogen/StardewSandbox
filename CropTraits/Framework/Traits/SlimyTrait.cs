using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;

namespace CropTraits.Framework.Traits
{
    public class SlimyTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Slimy";


        public override string Id => ID;
        public override TraitPolarity Polarity => TraitPolarity.Neutral;
        public override TraitRarity Rarity => TraitRarity.Uncommon;
        public override bool CanBeRolled => MonsterHelper.AreMonsterTraitsEnabled;

        public override void OnDayStarted(MorningContext context, int tier)
        {
            if (MonsterHelper.ShouldSpawn(context) is false)
            {
                return;
            }

            GreenSlime slime = new GreenSlime(MonsterHelper.GetSpawnPosition(context.Tile), MonsterHelper.GetMineLevel(tier));

            // The slime takes on the crop's color, keeping its own when the crop has none
            Color cropColor = MonsterHelper.GetCropColor(context.Dirt.crop);
            if (cropColor != Color.White)
            {
                slime.color.Value = cropColor;
            }

            MonsterHelper.Spawn(context.Location, slime);
        }

        public override string GetTierDetails(int tier)
        {
            return GetText($"details.tier_{MonsterHelper.GetTierIndex(tier) + 1}", new { spawnChance = ToPercentValue(MonsterHelper.SPAWN_CHANCE) });
        }

        public override string GetNotes()
        {
            return GetText("notes", new { monsterNotes = MonsterHelper.GetNotes() });
        }
    }
}
