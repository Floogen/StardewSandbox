using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using System.Collections.Generic;

namespace CropTraits.Framework.Patches
{
    public class MachineDataUtilityPatch : PatchTemplate
    {
        private readonly System.Type _machineDataUtility = typeof(MachineDataUtility);

        public MachineDataUtilityPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_machineDataUtility, nameof(MachineDataUtility.GetOutputItem), null), postfix: new HarmonyMethod(GetType(), nameof(GetOutputItemPostfix)));
        }

        // Price traits carry into artisan goods made from traited produce
        private static void GetOutputItemPostfix(Item inputItem, MachineItemOutput outputData, Item __result)
        {
            if (__result is null || inputItem is null)
            {
                return;
            }

            // The Seed Maker writes its own traits, so outputs that already have traits are left alone
            if (__result.modData.ContainsKey(CropTraits.TRAITS_MOD_DATA_KEY))
            {
                return;
            }

            TraitSet inputTraits = TraitModDataHelper.GetTraits(inputItem.modData);
            if (inputTraits.Count == 0 || IsDerivedFromInput(__result, inputItem, outputData) is false)
            {
                return;
            }

            TraitSet priceTraits = new TraitSet();
            foreach (KeyValuePair<string, int> trait in inputTraits.Entries)
            {
                if (CropTraits.traitManager.IsPriceTrait(trait.Key))
                {
                    priceTraits.Set(trait.Key, trait.Value);
                }
            }

            if (priceTraits.Count > 0)
            {
                TraitModDataHelper.SetTraits(__result.modData, priceTraits);
            }
        }

        // Flavored goods (wine, jelly, juice) point back at their ingredient, other outputs must copy its price
        private static bool IsDerivedFromInput(Item output, Item inputItem, MachineItemOutput outputData)
        {
            if (output is Object outputObject && outputObject.preservedParentSheetIndex.Value == inputItem.ItemId)
            {
                return true;
            }

            return outputData is not null && outputData.CopyPrice;
        }
    }
}
