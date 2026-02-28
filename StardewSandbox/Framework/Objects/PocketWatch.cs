using Microsoft.Xna.Framework;
using StardewSandbox.Framework.Managers;
using StardewSandbox.Framework.Utilities;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StardewSandbox.Framework.Objects
{
    internal class PocketWatch
    {
        public int Charges
        {
            get
            {
                var clockworkCharges = GetClockworkCharges();
                if (clockworkCharges is not null)
                {
                    return clockworkCharges.Stack;
                }

                return 0;
            }
        }

        public bool IsActive { get; private set; }

        private Tool _actualTool;

        private const int REQUIRED_SECONDS_FOR_TIME_CHANGE = 7; // Game time is updated every 7 seconds (10 minute interval)
        private double _millisecondsBeforeNextChargeConsumed = 0;

        public PocketWatch (Tool tool)
        {
            _actualTool = tool;
        }

        internal void Activate()
        {
            if (IsActive)
            {
                return;
            }
            else if (Charges <= 0)
            {
                // TODO: Display message about lack of charges
                return;
            }
            ConsumeCharge();

            ChangeTimeState(isTimeFrozen: true);
        }

        internal void Deactivate()
        {
            ChangeTimeState(isTimeFrozen: false);
        }

        private Object GetClockworkCharges()
        {
            if (_actualTool is not null)
            {
                return _actualTool.attachments.FirstOrDefault();
            }

            return null;
        }

        internal void ConsumeCharge()
        {
            var clockworkCharges = GetClockworkCharges();
            if (clockworkCharges is not null)
            {
                clockworkCharges.ConsumeStack(1);

                _millisecondsBeforeNextChargeConsumed = REQUIRED_SECONDS_FOR_TIME_CHANGE * 1000;
            }
        }

        internal void Update(GameTime time)
        {
            if (IsActive)
            {
                // Validate if time would be paused without the pocket watch being active
                // If so, don't consume a charge
                Game1.isTimePaused = false;
                if (Game1.shouldTimePass() is false)
                {
                    Game1.isTimePaused = true;
                    return;
                }
                Game1.isTimePaused = true;

                if (Charges <= 0)
                {
                    ChangeTimeState(isTimeFrozen: false);
                    return;
                }

                _millisecondsBeforeNextChargeConsumed -= time.ElapsedGameTime.TotalMilliseconds;
                if (_millisecondsBeforeNextChargeConsumed <= 0)
                {
                    ConsumeCharge();
                }
            }
        }

        internal float GetScaleSize()
        {
            if (IsActive is false)
            {
                return 0.75f;
            }

            const float maxSize = 1f;
            const float minSize = 0.25f;

            float averageSize = (minSize + maxSize) / 2.0f;
            float frequency = 0.1f;

            float speed = 2f;
            float oscillation = (float)System.Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * speed * System.Math.PI);

            return averageSize + (frequency * oscillation);
        }

        private void ChangeTimeState(bool isTimeFrozen)
        {
            IsActive = isTimeFrozen;
            Game1.isTimePaused = isTimeFrozen;

            //Game1.addHUDMessage(HUDMessage.ForCornerTextbox($"Time is now {(isTimeFrozen ? "frozen" : "unfrozen")}"));
        }

        internal bool HasToolInstance(Tool tool)
        {
            if (_actualTool is null)
            {
                return false;
            }

            return tool == _actualTool;
        }

        internal static bool IsToolPocketWatch(Tool tool)
        {
            if (tool is null || tool.attachments is null || tool.modData.ContainsKey(ModDataKeys.POCKET_WATCH_ACTIVE_FLAG) is false)
            {
                return false;
            }

            return true;
        }

        internal static bool IsFuel(Object item)
        {
            if (item is null || item.ItemId != AssetManager.CLOCKWORK_GEAR_GOLD_OBJECT_DATA_ID)
            {
                return false;
            }

            return true;
        }
    }
}
