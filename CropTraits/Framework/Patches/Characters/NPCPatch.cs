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
            harmony.Patch(AccessTools.Method(_npc, nameof(NPC.getGiftTasteForThisItem), null), postfix: new HarmonyMethod(GetType(), nameof(GetGiftTasteForThisItemPostfix)));
        }

        // Traits like Charmed change the taste itself, so the villager's reaction and dialogue follow along
        // The item is read by position (__0), so a renamed game parameter can't break the patch
        private static void GetGiftTasteForThisItemPostfix(Item __0, ref int __result)
        {
            if (__0 is null || TraitModDataHelper.HasTraits(__0.modData) is false)
            {
                return;
            }

            __result = CropTraits.traitManager.GetGiftTaste(TraitModDataHelper.GetTraits(__0.modData), __result);
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

            // Traits like Acquired Taste make a bad gift worse, while the usual friendship traits only ever touch good gifts
            int giftTaste = __instance.getGiftTasteForThisItem(o);
            if (giftTaste == NPC.gift_taste_dislike || giftTaste == NPC.gift_taste_hate)
            {
                friendshipChangeMultiplier *= (float)CropTraits.traitManager.GetBadGiftFriendshipMultiplier(traits);
                return;
            }

            friendshipChangeMultiplier *= (float)CropTraits.traitManager.GetGiftFriendshipMultiplier(traits);
        }
    }
}
