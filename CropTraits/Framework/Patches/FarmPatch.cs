using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CropTraits.Framework.Patches
{
    public class FarmPatch : PatchTemplate
    {
        private readonly System.Type _farm = typeof(Farm);
        private static readonly MethodInfo _destroyCropMethod = AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.destroyCrop));

        public FarmPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_farm, nameof(Farm.addCrows), null), transpiler: new HarmonyMethod(GetType(), nameof(AddCrowsTranspiler)));
        }

        // Passes the game's scarecrow result through CrowHelper right before a crow eats, so traits can protect or expose the crop
        private static IEnumerable<CodeInstruction> AddCrowsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            // Matches "if (!scarecrow) { dirt.destroyCrop(false); spawnCrowEvent.Fire(tile); }"
            int destroyIndex = codes.FindIndex(code => code.Calls(_destroyCropMethod));
            int fireIndex = destroyIndex < 0 ? -1 : codes.FindIndex(destroyIndex, code => code.operand is MethodInfo method && method.Name == "Fire");

            bool isExpectedShape = destroyIndex >= 4 && fireIndex > 0 && codes[destroyIndex - 4].IsLdloc() && codes[destroyIndex - 3].Branches(out _) && codes[destroyIndex - 2].IsLdloc() && codes[fireIndex - 1].IsLdloc();
            if (isExpectedShape is false)
            {
                _monitor.Log("Couldn't find the crow check in Farm.addCrows, so traits like Thorny won't change which crops crows eat.", LogLevel.Warn);
                return codes;
            }

            // The scarecrow result is already on the stack, so the dirt, tile and farm are added before the branch reads it
            CodeInstruction dirtLoad = codes[destroyIndex - 2];
            CodeInstruction tileLoad = codes[fireIndex - 1];
            codes.InsertRange(destroyIndex - 3, new[] { new CodeInstruction(dirtLoad.opcode, dirtLoad.operand), new CodeInstruction(tileLoad.opcode, tileLoad.operand), new CodeInstruction(OpCodes.Ldarg_0), CodeInstruction.Call(typeof(CrowHelper), nameof(CrowHelper.IsSafeFromCrow)) });

            return codes;
        }
    }
}
