﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using static StardewValley.FarmerSprite;

namespace StardewSandbox.Framework.Interfaces
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