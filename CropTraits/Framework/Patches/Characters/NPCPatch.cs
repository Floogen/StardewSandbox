using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CropTraits.Framework.Patches.Characters
{
    public class NPCPatch : PatchTemplate
    {
        private readonly System.Type _npc = typeof(NPC);

        public NPCPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_npc, nameof(NPC.receiveGift), null), prefix: new HarmonyMethod(GetType(), nameof(ReceiveGiftPrefix)));
        }

        // Traits like Charming scale the friendship from a gift the villager doesn't dislike or hate, so they never make a bad gift worse
        private static void ReceiveGiftPrefix(NPC __instance, StardewValley.Object o, ref float friendshipChangeMultiplier)
        {
            if (o is null)
            {
                return;
            }

            TraitSet traits = TraitModDataHelper.GetTraits(o.modData);
            if (traits.Count == 0)
            {
                return;
            }

            int giftTaste = __instance.getGiftTasteForThisItem(o);
            if (giftTaste == NPC.gift_taste_dislike || giftTaste == NPC.gift_taste_hate)
            {
                return;
            }

            friendshipChangeMultiplier *= (float)CropTraits.traitManager.GetGiftFriendshipMultiplier(traits);
        }
    }
}
