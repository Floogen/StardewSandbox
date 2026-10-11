using CropTraits.Framework.Models;
using StardewModdingAPI;
using StardewValley;
using System.Linq;

namespace CropTraits.Framework.Utilities.Helpers
{
    /// <summary>
    /// Tracks a running splice on the Seed Splicer itself, so it keeps going when the menu closes or the player leaves.
    /// The finished seeds wait in the machine's heldObject (saved by the game), and the start day and time live in its modData.
    /// </summary>
    public static class SpliceHelper
    {
        public const int SPLICE_DURATION_MINUTES = 180;
        public const int LAST_TIME_OF_DAY = 2600;

        // Another player has the menu open while they're online and still in the same location, so a claim left by someone who disconnected or walked away never blocks it
        public static bool IsInUseByAnotherPlayer(Object machine, GameLocation location)
        {
            if (machine.modData.TryGetValue(CropTraits.SPLICER_USER_MOD_DATA_KEY, out string userIdText) is false || long.TryParse(userIdText, out long userId) is false || userId == Game1.player.UniqueMultiplayerID)
            {
                return false;
            }

            return Game1.getOnlineFarmers().Any(farmer => farmer.UniqueMultiplayerID == userId && farmer.currentLocation?.NameOrUniqueName == location?.NameOrUniqueName);
        }

        public static void ClaimMachine(Object machine)
        {
            machine.modData[CropTraits.SPLICER_USER_MOD_DATA_KEY] = Game1.player.UniqueMultiplayerID.ToString();
        }

        // Only the player holding the claim releases it, so closing a menu never frees one someone else has open
        public static void ReleaseMachine(Object machine)
        {
            if (machine.modData.TryGetValue(CropTraits.SPLICER_USER_MOD_DATA_KEY, out string userIdText) && userIdText == Game1.player.UniqueMultiplayerID.ToString())
            {
                machine.modData.Remove(CropTraits.SPLICER_USER_MOD_DATA_KEY);
            }
        }

        public static bool IsSplicing(Object machine)
        {
            return machine?.heldObject.Value is not null && IsReady(machine) is false;
        }

        public static bool HasOutput(Object machine)
        {
            return machine?.heldObject.Value is not null;
        }

        public static bool IsReady(Object machine)
        {
            if (machine?.heldObject.Value is null)
            {
                return false;
            }

            // Output without a start time (such as one missing its modData) is treated as finished, so it can always be collected
            if (TryGetStart(machine, out int startDay, out int startTime) is false)
            {
                return true;
            }

            // The day ends at 2am, so a splice started on an earlier day has always finished overnight
            if (Game1.Date.TotalDays > startDay)
            {
                return true;
            }

            return GetElapsedMinutes(startTime) >= SPLICE_DURATION_MINUTES;
        }

        // Progress for the status line, from 0 to 99 while the splice runs
        public static int GetProgressPercent(Object machine)
        {
            if (IsReady(machine) || TryGetStart(machine, out _, out int startTime) is false)
            {
                return 100;
            }

            int plannedMinutes = System.Math.Min(SPLICE_DURATION_MINUTES, ToMinutes(LAST_TIME_OF_DAY) - ToMinutes(startTime));
            if (plannedMinutes <= 0)
            {
                return 99;
            }

            return System.Math.Clamp(GetElapsedMinutes(startTime) * 100 / plannedMinutes, 0, 99);
        }

        public static void StartSplice(Object machine, Object seeds, TraitSet upgradedTraits)
        {
            machine.heldObject.Value = seeds;
            machine.MinutesUntilReady = SPLICE_DURATION_MINUTES;
            machine.readyForHarvest.Value = false;
            machine.modData[CropTraits.SPLICE_START_DAY_MOD_DATA_KEY] = Game1.Date.TotalDays.ToString();
            machine.modData[CropTraits.SPLICE_START_TIME_MOD_DATA_KEY] = Game1.timeOfDay.ToString();

            // Kept so the upgrade note can be shown when the seeds are collected
            if (upgradedTraits is not null && upgradedTraits.Count > 0)
            {
                machine.modData[CropTraits.SPLICE_UPGRADED_TRAITS_MOD_DATA_KEY] = upgradedTraits.ToKey();
            }
            else
            {
                machine.modData.Remove(CropTraits.SPLICE_UPGRADED_TRAITS_MOD_DATA_KEY);
            }
        }

        public static TraitSet GetUpgradedTraits(Object machine)
        {
            return machine.modData.TryGetValue(CropTraits.SPLICE_UPGRADED_TRAITS_MOD_DATA_KEY, out string traitKey) ? TraitSet.FromKey(traitKey) : new TraitSet();
        }

        public static void ClearSplice(Object machine)
        {
            machine.heldObject.Value = null;
            machine.MinutesUntilReady = 0;
            machine.readyForHarvest.Value = false;
            machine.modData.Remove(CropTraits.SPLICE_START_DAY_MOD_DATA_KEY);
            machine.modData.Remove(CropTraits.SPLICE_START_TIME_MOD_DATA_KEY);
            machine.modData.Remove(CropTraits.SPLICE_UPGRADED_TRAITS_MOD_DATA_KEY);
        }

        public static bool TryGetStart(Object machine, out int startDay, out int startTime)
        {
            startTime = 0;
            if (machine.modData.TryGetValue(CropTraits.SPLICE_START_DAY_MOD_DATA_KEY, out string startDayText) is false || int.TryParse(startDayText, out startDay) is false)
            {
                startDay = 0;
                return false;
            }

            return machine.modData.TryGetValue(CropTraits.SPLICE_START_TIME_MOD_DATA_KEY, out string startTimeText) && int.TryParse(startTimeText, out startTime);
        }

        public static int GetElapsedMinutes(int startTime)
        {
            return ToMinutes(Game1.timeOfDay) - ToMinutes(startTime);
        }

        public static int ToMinutes(int timeOfDay)
        {
            return ((timeOfDay / 100) * 60) + (timeOfDay % 100);
        }

        // Keeps the vanilla ready bubble in sync with the stored ready time
        public static void UpdateReadyState(Object machine)
        {
            if (machine?.QualifiedItemId != CropTraits.SEED_SPLICER_QUALIFIED_ID || machine.heldObject.Value is null)
            {
                return;
            }

            bool isReady = IsReady(machine);
            if (machine.readyForHarvest.Value != isReady)
            {
                machine.readyForHarvest.Value = isReady;
            }

            if (isReady && machine.MinutesUntilReady > 0)
            {
                machine.MinutesUntilReady = 0;
            }
        }

        // The host owns machine state, and the bubble only needs checking whenever the clock moves
        public static void UpdateAllReadyStates()
        {
            if (Context.IsMainPlayer is false)
            {
                return;
            }

            Utility.ForEachLocation(location =>
            {
                foreach (Object machine in location.objects.Values)
                {
                    UpdateReadyState(machine);
                }

                return true;
            });
        }
    }
}
