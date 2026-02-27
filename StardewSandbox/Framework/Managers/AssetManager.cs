using StardewModdingAPI;
using StardewSandbox.Framework.Utilities;
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
        public const string POCKET_WATCH_TEXTURE_PATH = "PocketWatch/Textures/PocketWatch";

        public AssetManager(IModHelper helper) : base(helper)
        {

        }

        internal void AddToolData(IAssetData gameToolData)
        {
            IDictionary<string, ToolData> toolData = gameToolData.AsDictionary<string, ToolData>().Data;

            string fullId = $"PeacefulEnd.PocketWatch_PocketWatchTool";
            ToolData newToolData = new()
            {
                ClassName = "GenericTool",
                Name = fullId,
                SalePrice = 750,
                DisplayName = "Pocket Watch",
                Description = "...",
                Texture = POCKET_WATCH_TEXTURE_PATH,
                AttachmentSlots = 1,
                ModData = new()
                {
                    [ModDataKeys.POCKET_WATCH_ACTIVE_FLAG] = true.ToString()
                }
            };

            toolData[fullId] = newToolData;
        }

        internal string GetPocketWatchAssetPath()
        {
            return Path.Combine(_modHelper.ModContent.GetInternalAssetName(Path.Combine("Framework", "Assets")).Name, "PocketWatch.png");
        }
    }
}
