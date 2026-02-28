using StardewModdingAPI;
using StardewSandbox.Framework.Utilities;
using StardewValley;
using StardewValley.GameData.Museum;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSandbox.Framework.Managers
{
    public class AssetManager : BaseManager
    {
        public const string POCKET_WATCH_TOOL_TEXTURE_PATH = "PocketWatch/Textures/Tools/PocketWatch";
        public const string POCKET_WATCH_ARTIFACT_TEXTURE_PATH = "PocketWatch/Textures/Artifacts/PocketWatch";
        public const string CLOCKWORK_GEAR_GOLD_OBJECT_TEXTURE_PATH = "PocketWatch/Textures/Objects/ClockworkGearGold";

        public const string POCKET_WATCH_TOOL_DATA_ID = "PeacefulEnd.PocketWatch_Tools_PocketWatch";
        public const string POCKET_WATCH_ARTIFACT_DATA_ID = "PeacefulEnd.PocketWatch_Artifact_PocketWatch";
        public const string CLOCKWORK_GEAR_GOLD_OBJECT_DATA_ID = "PeacefulEnd.PocketWatch_Objects_Clockwork_Gear_Gold";

        private Dictionary<string, string> _assetNameToPath;

        public AssetManager(IModHelper helper) : base(helper)
        {
            var assetFolderPath = _modHelper.ModContent.GetInternalAssetName(Path.Combine("Framework", "Assets")).Name;
            _assetNameToPath = new Dictionary<string, string>()
            {
                { POCKET_WATCH_TOOL_TEXTURE_PATH, Path.Combine(assetFolderPath, "PocketWatch.png") },
                { POCKET_WATCH_ARTIFACT_TEXTURE_PATH, Path.Combine(assetFolderPath, "PocketWatchArtifact.png") },
                { CLOCKWORK_GEAR_GOLD_OBJECT_TEXTURE_PATH, Path.Combine(assetFolderPath, "ClockworkGearGold.png") },
            };
        }

        internal void AddToolData(IAssetData gameToolData)
        {
            IDictionary<string, ToolData> toolData = gameToolData.AsDictionary<string, ToolData>().Data;

            string fullId = POCKET_WATCH_TOOL_DATA_ID;
            ToolData additionalToolData = new()
            {
                ClassName = "GenericTool",
                Name = fullId,
                SalePrice = 750,
                DisplayName = "Pocket Watch",
                Description = "...",
                Texture = POCKET_WATCH_TOOL_TEXTURE_PATH,
                AttachmentSlots = 1,
                ModData = new()
                {
                    [ModDataKeys.POCKET_WATCH_ACTIVE_FLAG] = true.ToString()
                }
            };

            toolData[fullId] = additionalToolData;
        }

        internal void AddArtifactData(IAssetData gameObjectData)
        {
            IDictionary<string, ObjectData> objectData = gameObjectData.AsDictionary<string, ObjectData>().Data;

            string fullId = POCKET_WATCH_ARTIFACT_DATA_ID;
            ObjectData additionalObjectData = new()
            {
                Name = fullId,
                DisplayName = "Broken Pocket Watch",
                Description = "...",
                Type = "Arch",
                Category = 0,
                Texture = POCKET_WATCH_ARTIFACT_TEXTURE_PATH,
                Price = 1,
                Edibility = -300,
                IsDrink = false,
                GeodeDropsDefaultItems = false,
                GeodeDrops = null,
                ArtifactSpotChances = new Dictionary<string, float>()
                {
                    { "Mountain", 0.01f },
                    { "UndergroundMine", 0.02f },
                }
            };

            objectData[fullId] = additionalObjectData;
        }

        internal void AddObjectData(IAssetData gameObjectData)
        {
            IDictionary<string, ObjectData> objectData = gameObjectData.AsDictionary<string, ObjectData>().Data;

            string fullId = CLOCKWORK_GEAR_GOLD_OBJECT_DATA_ID;
            ObjectData additionalObjectData = new()
            {
                Name = fullId,
                DisplayName = "Golden Clockwork Gear",
                Description = "...",
                Type = "Crafting",
                Category = -8,
                Texture = CLOCKWORK_GEAR_GOLD_OBJECT_TEXTURE_PATH,
                Price = 1,
                Edibility = -300,
                IsDrink = false,
                GeodeDropsDefaultItems = false,
                GeodeDrops = null
            };

            objectData[fullId] = additionalObjectData;
        }

        internal void AddMuseumRewardsData(IAssetData gameMuseumData)
        {
            IDictionary<string, MuseumRewards> museumRewardsData = gameMuseumData.AsDictionary<string, MuseumRewards>().Data;

            string pocketWatchToolReward = $"PeacefulEnd.PocketWatch_MuseumRewards_PocketWatch";
            museumRewardsData[pocketWatchToolReward] = new()
            {
                TargetContextTags = new List<MuseumDonationRequirement>()
                {
                    new MuseumDonationRequirement()
                    {
                        Tag = $"id_o_{POCKET_WATCH_ARTIFACT_DATA_ID}",
                        Count = 1
                    }
                },
                RewardItemId = POCKET_WATCH_TOOL_DATA_ID,
                RewardItemCount = 1,
                RewardItemIsSpecial = true,
                RewardItemIsRecipe = false
            };

            string pocketWatchRecipeReward = $"PeacefulEnd.PocketWatch_MuseumRewards_PocketWatch_Recipe";
            museumRewardsData[pocketWatchRecipeReward] = new()
            {
                TargetContextTags = new List<MuseumDonationRequirement>()
                {
                    new MuseumDonationRequirement()
                    {
                        Tag = $"id_o_{POCKET_WATCH_ARTIFACT_DATA_ID}",
                        Count = 1
                    }
                },
                RewardItemId = POCKET_WATCH_TOOL_DATA_ID,
                RewardItemCount = 1,
                RewardItemIsSpecial = true,
                RewardItemIsRecipe = true
            };

            string clockworkGearGoldRecipeReward = $"PeacefulEnd.PocketWatch_MuseumRewards_ClockworkGear_Gold_Recipe";
            museumRewardsData[clockworkGearGoldRecipeReward] = new()
            {
                TargetContextTags = new List<MuseumDonationRequirement>()
                {
                    new MuseumDonationRequirement()
                    {
                        Tag = $"id_o_{POCKET_WATCH_ARTIFACT_DATA_ID}",
                        Count = 1
                    }
                },
                RewardItemId = CLOCKWORK_GEAR_GOLD_OBJECT_DATA_ID,
                RewardItemCount = 1,
                RewardItemIsSpecial = true,
                RewardItemIsRecipe = true
            };
        }

        internal void AddCraftingRecipesData(IAssetData gameMuseumData)
        {
            IDictionary<string, string> craftingRecipesData = gameMuseumData.AsDictionary<string, string>().Data;

            craftingRecipesData[POCKET_WATCH_TOOL_DATA_ID] = $"{POCKET_WATCH_ARTIFACT_DATA_ID} 1/Home/{POCKET_WATCH_TOOL_DATA_ID}/false/null/";
            craftingRecipesData[CLOCKWORK_GEAR_GOLD_OBJECT_DATA_ID] = $"(O)336 1/Home/{CLOCKWORK_GEAR_GOLD_OBJECT_DATA_ID} 2/false/null/";
        }

        internal bool HasCustomAsset(IAssetName assetName, out string assetPath)
        {
            assetPath = null;
            foreach (var key in _assetNameToPath.Keys)
            {
                if (assetName.IsEquivalentTo(key))
                {
                    assetPath = _assetNameToPath[key];
                }
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            return true;
        }
    }
}
