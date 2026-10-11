using CropTraits.Framework.Managers;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CropTraits.Framework.Utilities.Helpers
{
    public static class FertilizerHelper
    {
        // Basic, Quality then Deluxe Fertilizer, ranked weakest to strongest so traits like Composting only ever upgrade soil
        public static readonly string[] QUALITY_FERTILIZER_IDS = { "(O)368", "(O)369", "(O)919" };

        public const int NO_FERTILIZER_RANK = 0;
        public const int OTHER_FERTILIZER_RANK = -1;

        // Fertilizer may be stored qualified or unqualified. Fertilizers outside the quality line (like Speed-Gro) rank as other
        public static int GetQualityFertilizerRank(string fertilizerId)
        {
            if (string.IsNullOrEmpty(fertilizerId))
            {
                return NO_FERTILIZER_RANK;
            }

            int index = System.Array.IndexOf(QUALITY_FERTILIZER_IDS, ItemRegistry.QualifyItemId(fertilizerId));
            return index < 0 ? OTHER_FERTILIZER_RANK : index + 1;
        }

        // Uses the game's own name for the fertilizer, so it matches the player's language
        public static string GetQualityFertilizerName(int rank)
        {
            return ItemRegistry.GetDataOrErrorItem(QUALITY_FERTILIZER_IDS[System.Math.Clamp(rank, 1, QUALITY_FERTILIZER_IDS.Length) - 1]).DisplayName;
        }

        // Only soil with no fertilizer or a weaker quality fertilizer is upgraded, so other fertilizers are never replaced
        public static bool TryUpgradeQualityFertilizer(HoeDirt dirt, int rank)
        {
            int currentRank = GetQualityFertilizerRank(dirt?.fertilizer.Value);
            int targetRank = System.Math.Clamp(rank, 1, QUALITY_FERTILIZER_IDS.Length);
            if (dirt is null || currentRank == OTHER_FERTILIZER_RANK || currentRank >= targetRank)
            {
                return false;
            }

            dirt.fertilizer.Value = QUALITY_FERTILIZER_IDS[targetRank - 1];
            return true;
        }

        public const int FERTILIZER_DRAW_METHOD_COUNT = 2; // HoeDirt.DrawOptimized and IndoorPot.draw

        // Tracked by name, since Harmony reruns transpilers whenever another mod patches the same method
        private static readonly HashSet<string> _swappedMethodNames = new HashSet<string>();
        private static readonly MethodInfo _getFertilizerSourceRect = AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.GetFertilizerSourceRect));
        private static readonly FieldInfo _mouseCursors = AccessTools.Field(typeof(Game1), nameof(Game1.mouseCursors));

        // The custom overlay is only used when every fertilizer draw was swapped, since a missed one would draw the wrong part of mouseCursors
        public static bool IsTextureSwapActive => _swappedMethodNames.Count >= FERTILIZER_DRAW_METHOD_COUNT;

        // Called by the transpiled draw methods in place of Game1.mouseCursors, with the HoeDirt or IndoorPot doing the drawing
        public static Texture2D GetFertilizerTexture(object owner)
        {
            HoeDirt dirt = owner is IndoorPot pot ? pot.hoeDirt.Value : owner as HoeDirt;
            if (IsTextureSwapActive && dirt is not null && MutationHelper.HasMutagenicFertilizer(dirt))
            {
                return Game1.content.Load<Texture2D>(AssetManager.SPRITE_SHEET_TEXTURE_PATH);
            }

            return Game1.mouseCursors;
        }

        public static IEnumerable<CodeInstruction> ReplaceFertilizerTexture(IEnumerable<CodeInstruction> instructions, IMonitor monitor, string methodName, bool isTextureLoadedBeforeSourceRect)
        {
            CodeMatcher matcher = new CodeMatcher(instructions).MatchStartForward(new CodeMatch(instruction => instruction.Calls(_getFertilizerSourceRect)));
            if (matcher.IsValid && isTextureLoadedBeforeSourceRect)
            {
                matcher.MatchStartBackwards(new CodeMatch(instruction => instruction.LoadsField(_mouseCursors)));
            }
            else if (matcher.IsValid)
            {
                matcher.MatchStartForward(new CodeMatch(instruction => instruction.LoadsField(_mouseCursors)));
            }

            if (matcher.IsInvalid)
            {
                monitor.Log($"Couldn't find the fertilizer draw in {methodName}, so Mutagenic Fertilizer will use the vanilla fertilizer sprite there.", LogLevel.Warn);
                return instructions;
            }

            // Labels and exception blocks move to the new first instruction, so jumps into this spot still land correctly
            CodeInstruction textureLoad = matcher.Instruction;
            matcher.SetInstruction(new CodeInstruction(OpCodes.Ldarg_0).WithLabels(textureLoad.ExtractLabels()).WithBlocks(textureLoad.ExtractBlocks()));
            matcher.Advance(1).Insert(CodeInstruction.Call(typeof(FertilizerHelper), nameof(GetFertilizerTexture)));
            _swappedMethodNames.Add(methodName);

            return matcher.InstructionEnumeration();
        }
    }
}
