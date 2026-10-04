using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Managers
{
    public class DiscoveryManager : BaseManager
    {
        public const string SAVE_DATA_KEY = "Discoveries";
        public const string REPORT_MESSAGE_TYPE = "DiscoveryReport";

        public DiscoveryData Data { get; private set; } = new();

        /// <summary>Tracks discoveries shared across the save. Farmhands also report discoveries to it.</summary>
        public DiscoveryManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public void Load()
        {
            Data = Context.IsMainPlayer ? helper.Data.ReadSaveData<DiscoveryData>(SAVE_DATA_KEY) ?? new DiscoveryData() : new DiscoveryData();
        }

        public void Save()
        {
            if (Context.IsMainPlayer)
            {
                helper.Data.WriteSaveData(SAVE_DATA_KEY, Data);
            }
        }

        public void Reset()
        {
            Data = new DiscoveryData();
        }

        public void RecordInventoryChanges(InventoryChangedEventArgs e)
        {
            if (e.IsLocalPlayer is false)
            {
                return;
            }

            foreach (Item item in e.Added)
            {
                TraitSet traits = TraitModDataHelper.GetTraits(item?.modData);
                if (traits.Count == 0)
                {
                    continue;
                }

                RecordTraits(traits, GetDiscoverySeedId(item));
            }
        }

        public void HandleModMessage(ModMessageReceivedEventArgs e)
        {
            if (Context.IsMainPlayer is false || e.FromModID != CropTraits.manifest.UniqueID || e.Type != REPORT_MESSAGE_TYPE)
            {
                return;
            }

            DiscoveryReportData report = e.ReadAs<DiscoveryReportData>();
            if (report is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(report.TraitKey) is false)
            {
                RecordTraits(TraitSet.FromKey(report.TraitKey), report.SeedId);
            }

            if (report.PairKey is not null && report.JournalEntry is not null)
            {
                RecordBlend(report.PairKey, report.JournalEntry, report.FusedCombinationKeys ?? new List<string>());
            }
        }

        // Safe to call repeatedly, since only unseen traits and tiers are added
        public void RecordTraits(TraitSet traits, string seedId)
        {
            if (traits is null || traits.Count == 0)
            {
                return;
            }

            if (Context.IsMainPlayer is false)
            {
                SendReport(new DiscoveryReportData { TraitKey = traits.ToKey(), SeedId = seedId });
                return;
            }

            bool hasNewDiscovery = false;
            foreach (KeyValuePair<string, int> trait in traits.Entries)
            {
                if (Data.Traits.TryGetValue(trait.Key, out TraitDiscoveryData discovery) is false)
                {
                    discovery = new TraitDiscoveryData { FirstSeedId = seedId, FirstDiscoveredDay = Game1.Date.TotalDays };
                    Data.Traits[trait.Key] = discovery;
                    hasNewDiscovery = true;
                }

                hasNewDiscovery |= discovery.TiersSeen.Add(trait.Value);
            }

            // Inventory changes call this constantly, so the journal is only marked stale when something was actually new
            if (hasNewDiscovery)
            {
                CropTraits.journalBuilder?.QueueRefresh();
            }
        }

        public void RecordBlend(string pairKey, BlendJournalEntryData entry, IEnumerable<string> fusedCombinationKeys)
        {
            // Farmhands also keep a copy for the session, so the Seed Splicer can preview their own results right away
            Data.BlendJournal[pairKey] = entry;

            foreach (string combinationKey in fusedCombinationKeys)
            {
                Data.Combinations.Add(combinationKey);
            }

            CropTraits.journalBuilder?.QueueRefresh();

            if (Context.IsMainPlayer is false)
            {
                SendReport(new DiscoveryReportData { PairKey = pairKey, JournalEntry = entry, FusedCombinationKeys = fusedCombinationKeys.ToList() });
            }
        }

        private void SendReport(DiscoveryReportData report)
        {
            try
            {
                helper.Multiplayer.SendMessage(report, REPORT_MESSAGE_TYPE, new[] { CropTraits.manifest.UniqueID }, new[] { Game1.MasterPlayer.UniqueMultiplayerID });
            }
            catch (Exception exception)
            {
                monitor.Log($"Failed to send discovery report to the host: {exception}", LogLevel.Warn);
            }
        }

        // Seeds identify their own crop, produce carries the seed it was grown from
        private string GetDiscoverySeedId(Item item)
        {
            if (item.modData.TryGetValue(CropTraits.SEED_ID_MOD_DATA_KEY, out string seedId) && string.IsNullOrEmpty(seedId) is false)
            {
                return seedId;
            }

            return item.Category == StardewValley.Object.SeedsCategory ? item.ItemId : CropTraits.blendManager.GetSeedIdByReverseLookup(item);
        }
    }
}
