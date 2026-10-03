using CropTraits.Framework.Utilities.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Objects;
using System.Collections.Generic;

namespace CropTraits.Framework.Patches.Objects
{
    public class IndoorPotPatch : PatchTemplate
    {
        private readonly System.Type _indoorPot = typeof(IndoorPot);

        public IndoorPotPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        public override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_indoorPot, nameof(IndoorPot.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }), transpiler: new HarmonyMethod(GetType(), nameof(DrawTranspiler)));
        }

        private static IEnumerable<CodeInstruction> DrawTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return FertilizerHelper.ReplaceFertilizerTexture(instructions, _monitor, $"{nameof(IndoorPot)}.{nameof(IndoorPot.draw)}", false);
        }
    }
}
