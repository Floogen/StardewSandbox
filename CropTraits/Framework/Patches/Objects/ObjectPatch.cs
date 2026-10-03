using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using CropTraits.Framework.UI.Menus;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.Objects;

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
            harmony.Patch(AccessTools.Method(_object, nameof(Object.drawInMenu), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }), prefix: new HarmonyMethod(GetType(), nameof(DrawInMenuPrefix)), postfix: new HarmonyMethod(GetType(), nameof(DrawInMenuPostfix)));
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

        private static void DrawInMenuPrefix(Object __instance, ref float scaleSize, out float __state)
        {
            __state = scaleSize;

            if (__instance is ColoredObject)
            {
                return;
            }

            float produceScale = ProduceScaleHelper.GetScale(__instance);
            if (produceScale == 1f)
            {
                return;
            }

            ProduceScaleHelper.BeginMenuDraw(__instance, scaleSize);
            scaleSize *= produceScale;
        }

        private static void DrawInMenuPostfix(Object __instance, SpriteBatch spriteBatch, Vector2 location, float transparency, float layerDepth, StackDrawType drawStackNumber, float __state)
        {
            if (__instance is ColoredObject)
            {
                return;
            }

            ProduceScaleHelper.EndMenuDraw(__instance);
            MutationBadgeHelper.DrawBadge(__instance, spriteBatch, location, __state, transparency, layerDepth, drawStackNumber);
        }

        private static void GetDescriptionPostfix(Object __instance, ref string __result)
        {
            if (TraitModDataHelper.HasTraits(__instance.modData) is false)
            {
                return;
            }

            string growthLine = GetGrowthLine(__instance);
            if (string.IsNullOrEmpty(growthLine) is false)
            {
                __result = $"{__result}{System.Environment.NewLine}{System.Environment.NewLine}{growthLine}";
            }
        }

        // The vanilla seed description states the base growth time, so seeds with growth traits add their adjusted time
        private static string GetGrowthLine(Object item)
        {
            if (item.Category != Object.SeedsCategory || Game1.cropData.TryGetValue(item.ItemId, out CropData cropData) is false)
            {
                return null;
            }

            int baseDays = GrowthHelper.GetBaseGrowthDays(cropData);
            int estimatedDays = GrowthHelper.GetEstimatedGrowthDays(cropData, TraitModDataHelper.GetTraits(item.modData));
            if (baseDays == 0 || estimatedDays == baseDays)
            {
                return null;
            }

            return Game1.parseText(TranslationHelper.Get("tooltip.growth_days", new { days = estimatedDays }), Game1.smallFont, DESCRIPTION_WIDTH);
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

            Game1.activeClickableMenu = new SeedSplicerMenu(__instance);
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
