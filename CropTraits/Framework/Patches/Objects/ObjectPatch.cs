using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using CropTraits.Framework.UI.Menus;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CropTraits.Framework.Patches.Objects
{
    public class ObjectPatch : PatchTemplate
    {
        public const int DESCRIPTION_WIDTH = 272;

        private readonly System.Type _object = typeof(Object);

        public ObjectPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.PropertyGetter(_object, nameof(Object.DisplayName)), postfix: new HarmonyMethod(GetType(), nameof(DisplayNamePostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.getDescription), null), postfix: new HarmonyMethod(GetType(), nameof(GetDescriptionPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.sellToStorePrice), null), postfix: new HarmonyMethod(GetType(), nameof(SellToStorePricePostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.checkForAction), null), prefix: new HarmonyMethod(GetType(), nameof(CheckForActionPrefix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.OutputSeedMaker), null), prefix: new HarmonyMethod(GetType(), nameof(OutputSeedMakerPrefix)), postfix: new HarmonyMethod(GetType(), nameof(OutputSeedMakerPostfix)));
        }

        // Seeds and produce get a short tag (e.g. "Parsnip Seeds (Hybrid)") since the tooltip lists the traits, while artisan goods name their price traits
        private static void DisplayNamePostfix(Object __instance, ref string __result)
        {
            string traitKey = TraitModDataHelper.GetTraitKey(__instance.modData);
            if (string.IsNullOrEmpty(traitKey))
            {
                return;
            }

            // Artisan goods are the only traited items without a seed ID, and they only ever carry price traits
            bool isSeed = __instance.Category == Object.SeedsCategory;
            bool isProduce = __instance.modData.ContainsKey(CropTraits.SEED_ID_MOD_DATA_KEY);
            if (isSeed is false && isProduce is false)
            {
                __result = $"{__result} ({CropTraits.traitManager.GetLabel(traitKey)})";
                return;
            }

            // Seedless is exclusive, so Seedless produce reads naturally as "Seedless Grapes"
            if (isProduce && TraitSet.FromKey(traitKey).Has(SeedlessTrait.ID))
            {
                __result = TranslationHelper.Get("item.name.seedless", new { name = __result });
                return;
            }

            __result = TranslationHelper.Get("item.name.hybrid", new { name = __result });
        }

        private static void GetDescriptionPostfix(Object __instance, ref string __result)
        {
            string label = CropTraits.traitManager.GetLabel(TraitModDataHelper.GetTraitKey(__instance.modData));
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            string traitLine = Game1.parseText(TranslationHelper.Get("tooltip.traits", new { traits = label }), Game1.smallFont, DESCRIPTION_WIDTH);
            __result = $"{__result}{System.Environment.NewLine}{System.Environment.NewLine}{traitLine}";
        }

        // Price traits apply at sale, so produce and artisan goods carrying them get the same effect
        private static void SellToStorePricePostfix(Object __instance, ref int __result)
        {
            // Price traits only affect produce and artisan goods, so traited seeds keep their vanilla price
            if (__result <= 0 || __instance.Category == Object.SeedsCategory)
            {
                return;
            }

            TraitSet traits = TraitModDataHelper.GetTraits(__instance.modData);
            if (traits.Count == 0)
            {
                return;
            }

            double multiplier = CropTraits.traitManager.GetPriceMultiplier(traits);
            if (multiplier == 1)
            {
                return;
            }

            __result = System.Math.Max(1, (int)System.Math.Round(__result * multiplier));
        }

        private static bool CheckForActionPrefix(Object __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
        {
            if (__instance.QualifiedItemId != CropTraits.SEED_SPLICER_QUALIFIED_ID)
            {
                return true;
            }

            __result = true;

            if (justCheckingForActivity || who is null || who.IsLocalPlayer is false || Game1.activeClickableMenu is not null)
            {
                return false;
            }

            Game1.activeClickableMenu = new SeedSplicerMenu();
            return false;
        }

        private static bool OutputSeedMakerPrefix(Item inputItem, ref Item __result)
        {
            // Null output makes the machine refuse the item
            if (inputItem is not null && CropTraits.blendManager.IsPropagationBlocked(inputItem))
            {
                __result = null;
                return false;
            }

            return true;
        }

        private static void OutputSeedMakerPostfix(Item inputItem, Item __result)
        {
            if (__result is null || inputItem is null)
            {
                return;
            }

            string traitKey = TraitModDataHelper.GetTraitKey(inputItem.modData);
            if (string.IsNullOrEmpty(traitKey) || inputItem.modData.TryGetValue(CropTraits.SEED_ID_MOD_DATA_KEY, out string seedId) is false)
            {
                return;
            }

            // Mixed Seeds, Ancient Seeds or any other output that isn't the crop's own seed stays untraited
            if (__result.QualifiedItemId != ItemRegistry.QualifyItemId(seedId))
            {
                return;
            }

            __result.modData[CropTraits.TRAITS_MOD_DATA_KEY] = traitKey;
        }
    }
}
