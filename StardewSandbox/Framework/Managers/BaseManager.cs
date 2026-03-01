using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimelessPocketWatch.Framework.Managers
{
    public abstract class BaseManager
    {
        protected IModHelper _modHelper;

        public BaseManager(IModHelper helper)
        {
            _modHelper = helper;
        }
    }
}
