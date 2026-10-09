using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using StardewValley.Monsters;

namespace CropTraits.Framework.Traits
{
    public class RoostingTrait : Trait
    {
        // Stored in every trait key, so this must never change once released
        public const string ID = "Roosting";

        public static readonly string[] BAT_NAMES_BY_TIER = { "bat", "frost bat", "lava bat" };

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

            MonsterHelper.Spawn(context.Location, new Bat(MonsterHelper.GetSpawnPosition(context.Tile), MonsterHelper.GetMineLevel(tier)));
        }

        public override string GetTierDetails(int tier)
        {
            return $"Has a {ToPercent(MonsterHelper.SPAWN_CHANCE)} chance each morning to spawn a {BAT_NAMES_BY_TIER[MonsterHelper.GetTierIndex(tier)]}";
        }

        public override string GetNotes()
        {
            return MonsterHelper.GetNotes();
        }
    }
}
