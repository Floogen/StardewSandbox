using CropTraits.Framework.Models;
using CropTraits.Framework.Traits;
using CropTraits.Framework.UI.Menus;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Crops;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CropTraits.Framework.Patches.Objects
{
    public class ObjectPatch : PatchTemplate
    {
        public const int DESCRIPTION_WIDTH = 272;

        private readonly System.Type _object = typeof(Object);
        private static readonly MethodInfo _colorWhiteGetter = AccessTools.PropertyGetter(typeof(Color), nameof(Color.White));

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
            harmony.Patch(AccessTools.Method(_object, nameof(Object.staminaRecoveredOnConsumption), null), postfix: new HarmonyMethod(GetType(), nameof(StaminaRecoveredOnConsumptionPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.healthRecoveredOnConsumption), null), prefix: new HarmonyMethod(GetType(), nameof(HealthRecoveredOnConsumptionPrefix)), postfix: new HarmonyMethod(GetType(), nameof(HealthRecoveredOnConsumptionPostfix)), finalizer: new HarmonyMethod(GetType(), nameof(HealthRecoveredOnConsumptionFinalizer)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.GetFoodOrDrinkBuffs), null), postfix: new HarmonyMethod(GetType(), nameof(GetFoodOrDrinkBuffsPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(float) }), transpiler: new HarmonyMethod(GetType(), nameof(DrawTranspiler)));
        }

        // Swaps the item sprite's Color.White (the last one before the last draw, after the shadow) for the produce's tint
        private static IEnumerable<CodeInstruction> DrawTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            int lastDrawIndex = codes.FindLastIndex(code => code.operand is MethodInfo method && method.DeclaringType == typeof(SpriteBatch) && method.Name == nameof(SpriteBatch.Draw));
            int whiteIndex = lastDrawIndex < 0 ? -1 : codes.FindLastIndex(lastDrawIndex, code => code.Calls(_colorWhiteGetter));
            if (whiteIndex < 0)
            {
                _monitor.Log("Couldn't find the item color in Object.draw, so traits like Variegated won't tint produce placed in the world.", LogLevel.Warn);
                return codes;
            }

            // Labels move to the new first instruction, so jumps into this spot still load the tint
            CodeInstruction whiteInstruction = codes[whiteIndex];
            codes[whiteIndex] = new CodeInstruction(OpCodes.Ldarg_0).WithLabels(whiteInstruction.ExtractLabels()).WithBlocks(whiteInstruction.ExtractBlocks());
            codes.Insert(whiteIndex + 1, CodeInstruction.Call(typeof(CropAppearanceHelper), nameof(CropAppearanceHelper.GetItemTint)));

            return codes;
        }

        // Food traits like Nourishing change the energy restored, which the tooltip reads too
        private static void StaminaRecoveredOnConsumptionPostfix(Object __instance, ref int __result)
        {
            __result = FoodHelper.ApplyEnergyTraits(__instance, __result);
        }

        // Health may be based on the energy value, so energy isn't adjusted while health is being worked out
        private static void HealthRecoveredOnConsumptionPrefix()
        {
            FoodHelper.BeginHealthCalculation();
        }

        private static void HealthRecoveredOnConsumptionPostfix(Object __instance, ref int __result)
        {
            __result = FoodHelper.ApplyRestorationTraits(__instance, __result);
        }

        private static void HealthRecoveredOnConsumptionFinalizer()
        {
            FoodHelper.EndHealthCalculation();
        }

        private static void GetFoodOrDrinkBuffsPostfix(Object __instance, ref IEnumerable<Buff> __result)
        {
            __result = FoodHelper.AddTraitBuffs(__instance, __result);
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

        private static void DrawInMenuPrefix(Object __instance, ref float scaleSize, ref Color color, out float __state)
        {
            __state = scaleSize;

            if (__instance is ColoredObject)
            {
                return;
            }

            // Traits like Variegated tint the produce icon
            color = CropAppearanceHelper.Multiply(color, CropAppearanceHelper.GetItemTint(__instance));

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

            // Seeds are already excluded, so a traited item without a seed ID is an artisan good
            bool isArtisanGood = __instance.modData.ContainsKey(CropTraits.SEED_ID_MOD_DATA_KEY) is false;

            double multiplier = CropTraits.traitManager.GetPriceMultiplier(traits, isArtisanGood);
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
