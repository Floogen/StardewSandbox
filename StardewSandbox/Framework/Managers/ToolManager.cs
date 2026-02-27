using StardewModdingAPI;
using StardewSandbox.Framework.Objects;
using StardewSandbox.Framework.Utilities;
using StardewValley;
using StardewValley.GameData.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSandbox.Framework.Managers
{
    public class ToolManager : BaseManager
    {
        private List<PocketWatch> _pocketWatches = new List<PocketWatch>();

        public ToolManager(IModHelper helper) : base(helper)
        {

        }

        internal PocketWatch GetPocketWatch(Tool tool)
        {
            if (_pocketWatches.Any(w => w.HasToolInstance(tool)) is false)
            {
                if (PocketWatch.IsToolPocketWatch(tool))
                {
                    var pocketWatch = new PocketWatch(tool);
                    _pocketWatches.Add(pocketWatch);

                    return pocketWatch;
                }

                return null;
            }
            else
            {
                return _pocketWatches.First(w => w.HasToolInstance(tool));
            }
        }
    }
}
