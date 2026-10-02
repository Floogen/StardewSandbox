using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System.Linq;
using System.Reflection;

namespace CropTraits.Framework.Patches
{
    public class ItemRegistryPatch : PatchTemplate
    {
        private readonly System.Type _itemRegistry = typeof(ItemRegistry);

        public ItemRegistryPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(GetCreateMethod(), postfix: new HarmonyMethod(GetType(), nameof(CreatePostfix)));
        }

        // Create has a generic Create<T> overload with the same parameters, so the non-generic one is selected explicitly (Create<T> calls it internally)
        private MethodInfo GetCreateMethod()
        {
            System.Type[] parameterTypes = { typeof(string), typeof(int), typeof(int), typeof(bool) };

            return AccessTools.GetDeclaredMethods(_itemRegistry).First(method => method.Name == nameof(ItemRegistry.Create) && method.IsGenericMethodDefinition is false && method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));
        }

        // Produce created during a traited harvest picks up the crop's traits
        private static void CreatePostfix(Item __result)
        {
            CropTraits.harvestManager.ApplyToHarvestedItem(__result);
        }
    }
}
