using StardewModdingAPI;

namespace CropTraits.Framework.Managers
{
    public class BaseManager
    {
        public IMonitor monitor;
        public IModHelper helper;

        public BaseManager(IMonitor monitor, IModHelper helper)
        {
            this.monitor = monitor;
            this.helper = helper;
        }
    }
}
