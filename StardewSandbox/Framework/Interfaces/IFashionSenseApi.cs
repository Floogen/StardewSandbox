﻿using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace HatShopRestoration.Framework.Interfaces
{
    public interface IFashionSenseApi
    {
        public enum Type
        {
            Unknown,
            Hair,
            Accessory,
            [Obsolete("No longer maintained. Use Accessory instead.")]
            AccessorySecondary,
            [Obsolete("No longer maintained. Use Accessory instead.")]
            AccessoryTertiary,
            Hat,
            Shirt,
            Pants,
            Sleeves,
            Shoes,
            Player
        }

        KeyValuePair<bool, string> SetAppearanceLockState(Type appearanceType, string targetPackId, string targetAppearanceName, bool isLocked, IManifest callerManifest);
    }
}