using CropTraits.Framework.Models;
using CropTraits.Framework.Models.Data;
using CropTraits.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CropTraits.Framework.Managers
{
    public class DiscoveryManager : BaseManager
    {
        public const string SAVE_DATA_KEY = "Discoveries";
        public const string REPORT_MESSAGE_TYPE = "DiscoveryReport"; // Farmhand to host, with one new discovery
        public const string UPDATE_MESSAGE_TYPE = "DiscoveryUpdate"; // Host to every farmhand, with one new discovery
        public const string SYNC_REQUEST_MESSAGE_TYPE = "DiscoverySyncRequest"; // Farmhand to host, asking for everything discovered so far
        public const string SYNC_MESSAGE_TYPE = "DiscoverySync"; // Host to one farmhand, with everything discovered so far

        // Per screen, so a split-screen farmhand never replaces the host's data held by the same game instance
        private readonly PerScreen<DiscoveryData> _data = new(() => new DiscoveryData());

        public DiscoveryData Data
        {
            get => _data.Value;
            private set => _data.Value = value;
        }

        /// <summary>Tracks discoveries shared across the save. Farmhands report new discoveries to the host, which shares them with every farmhand.</summary>
        public DiscoveryManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public void Load()
        {
            Data = Context.IsMainPlayer ? helper.Data.ReadSaveData<DiscoveryData>(SAVE_DATA_KEY) ?? new DiscoveryData() : new DiscoveryData();
            RecordPlayerInventory(Game1.player);

            // Farmhands start empty, so they ask the host for everything discovered so far (sent after any reports above, so the reply includes them)
            if (Context.IsMainPlayer is false)
            {
                RequestSync();
            }
        }

        public void RecordPlayerInventory(Farmer who)
        {
            if (who is null)
            {
                return;
            }

            foreach (Item item in who.Items)
            {
                RecordItem(item);
            }
        }

        public void RecordItem(Item item)
        {
            TraitSet traits = TraitModDataHelper.GetTraits(item?.modData);
            if (traits.Count == 0)
            {
                return;
            }

            RecordTraits(traits, GetDiscoverySeedId(item));
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
            _data.ResetAllScreens();
        }

        public void RecordInventoryChanges(InventoryChangedEventArgs e)
        {
            if (e.IsLocalPlayer is false)
            {
                return;
            }

            foreach (Item item in e.Added)
            {
                RecordItem(item);
            }

            // Items merging into a stack already held are reported as a size change rather than an addition
            foreach (ItemStackSizeChange change in e.QuantityChanged)
            {
                if (change.NewSize > change.OldSize)
                {
                    RecordItem(change.Item);
                }
            }
        }

        public void HandleModMessage(ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != CropTraits.manifest.UniqueID)
            {
                return;
            }

            if (Context.IsMainPlayer)
            {
                HandleHostMessage(e);
                return;
            }

            HandleFarmhandMessage(e);
        }

        // Reports are recorded and shared with every farmhand, while a sync request gets everything discovered so far
        private void HandleHostMessage(ModMessageReceivedEventArgs e)
        {
            if (e.Type == SYNC_REQUEST_MESSAGE_TYPE)
            {
                SendMessage(Data, SYNC_MESSAGE_TYPE, e.FromPlayerID);
                return;
            }

            if (e.Type == REPORT_MESSAGE_TYPE)
            {
                RecordReport(e.ReadAs<DiscoveryReportData>(), true);
            }
        }

        // Updates come from the host, so they're recorded without being sent back
        private void HandleFarmhandMessage(ModMessageReceivedEventArgs e)
        {
            if (e.Type == SYNC_MESSAGE_TYPE)
            {
                MergeSyncedData(e.ReadAs<DiscoveryData>());
                return;
            }

            if (e.Type == UPDATE_MESSAGE_TYPE)
            {
                RecordReport(e.ReadAs<DiscoveryReportData>(), false);
            }
        }

        private void RecordReport(DiscoveryReportData report, bool shouldShare)
        {
            if (report is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(report.TraitKey) is false)
            {
                RecordTraits(TraitSet.FromKey(report.TraitKey), report.SeedId, shouldShare);
            }

            if (report.PairKey is not null && report.JournalEntry is not null)
            {
                RecordBlend(report.PairKey, report.JournalEntry, report.FusedCombinationKeys ?? new List<string>(), shouldShare);
            }
        }

        // The host's first discovery details win, while tiers, crops, combinations and crossbreed results are combined with anything found this session
        public void MergeSyncedData(DiscoveryData syncedData)
        {
            if (syncedData is null)
            {
                return;
            }

            foreach (KeyValuePair<string, TraitDiscoveryData> syncedTrait in syncedData.Traits)
            {
                if (Data.Traits.TryGetValue(syncedTrait.Key, out TraitDiscoveryData localDiscovery))
                {
                    syncedTrait.Value.TiersSeen.UnionWith(localDiscovery.TiersSeen);
                }

                Data.Traits[syncedTrait.Key] = syncedTrait.Value;
            }

            foreach (KeyValuePair<string, CropDiscoveryData> syncedCrop in syncedData.Crops)
            {
                if (Data.Crops.TryGetValue(syncedCrop.Key, out CropDiscoveryData localCrop) is false)
                {
                    Data.Crops[syncedCrop.Key] = syncedCrop.Value;
                    continue;
                }

                foreach (KeyValuePair<string, HashSet<int>> syncedTiers in syncedCrop.Value.TraitTiers)
                {
                    if (localCrop.TraitTiers.TryGetValue(syncedTiers.Key, out HashSet<int> localTiers))
                    {
                        localTiers.UnionWith(syncedTiers.Value);
                        continue;
                    }

                    localCrop.TraitTiers[syncedTiers.Key] = syncedTiers.Value;
                }
            }

            Data.Combinations.UnionWith(syncedData.Combinations);
            foreach (KeyValuePair<string, BlendJournalEntryData> syncedEntry in syncedData.BlendJournal)
            {
                MergeBlendEntry(syncedEntry.Key, syncedEntry.Value);
            }

            CropTraits.journalBuilder?.QueueRefresh();
        }

        // Safe to call repeatedly, since only unseen traits and tiers are added
        public void RecordTraits(TraitSet traits, string seedId, bool shouldShare = true)
        {
            if (traits is null || traits.Count == 0)
            {
                return;
            }

            bool hasNewDiscovery = RecordCropTraits(seedId, traits);
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

            // Inventory changes call this constantly, so the journal is only marked stale and the host only told when something was actually new
            if (hasNewDiscovery is false)
            {
                return;
            }

            CropTraits.journalBuilder?.QueueRefresh();

            if (shouldShare)
            {
                ShareDiscovery(new DiscoveryReportData { TraitKey = traits.ToKey(), SeedId = seedId });
            }
        }

        // Returns whether any trait or tier was new for this crop
        private bool RecordCropTraits(string seedId, TraitSet traits)
        {
            if (string.IsNullOrEmpty(seedId) || traits is null || traits.Count == 0)
            {
                return false;
            }

            if (Data.Crops.TryGetValue(seedId, out CropDiscoveryData crop) is false)
            {
                crop = new CropDiscoveryData();
                Data.Crops[seedId] = crop;
            }

            bool hasNewTier = false;
            foreach (KeyValuePair<string, int> trait in traits.Entries)
            {
                if (crop.TraitTiers.TryGetValue(trait.Key, out HashSet<int> tiers) is false)
                {
                    tiers = new HashSet<int>();
                    crop.TraitTiers[trait.Key] = tiers;
                }

                hasNewTier |= tiers.Add(trait.Value);
            }

            return hasNewTier;
        }

        public void RecordBlend(string pairKey, BlendJournalEntryData entry, IEnumerable<string> fusedCombinationKeys, bool shouldShare = true)
        {
            // Farmhands also keep a copy, so the Seed Splicer knows right away which pairs they've made before
            MergeBlendEntry(pairKey, entry);

            foreach (string combinationKey in fusedCombinationKeys)
            {
                Data.Combinations.Add(combinationKey);
            }

            CropTraits.journalBuilder?.QueueRefresh();

            if (shouldShare)
            {
                ShareDiscovery(new DiscoveryReportData { PairKey = pairKey, JournalEntry = entry, FusedCombinationKeys = fusedCombinationKeys.ToList() });
            }
        }

        // A pair crossbred again keeps the earliest day it was crossbred and adds any result not seen before
        private void MergeBlendEntry(string pairKey, BlendJournalEntryData entry)
        {
            entry.ResultTraitKeys ??= new List<string>();
            if (Data.BlendJournal.TryGetValue(pairKey, out BlendJournalEntryData existingEntry))
            {
                entry.FirstSplicedDay = Math.Min(existingEntry.FirstSplicedDay, entry.FirstSplicedDay);
                entry.ResultTraitKeys = existingEntry.ResultTraitKeys.Union(entry.ResultTraitKeys).ToList();
            }

            Data.BlendJournal[pairKey] = entry;
        }

        // Farmhands tell the host, while the host tells every farmhand
        private void ShareDiscovery(DiscoveryReportData report)
        {
            if (Context.IsMultiplayer is false)
            {
                return;
            }

            if (Context.IsMainPlayer)
            {
                SendMessage(report, UPDATE_MESSAGE_TYPE, null);
                return;
            }

            SendMessage(report, REPORT_MESSAGE_TYPE, Game1.MasterPlayer.UniqueMultiplayerID);
        }

        private void RequestSync()
        {
            SendMessage(true, SYNC_REQUEST_MESSAGE_TYPE, Game1.MasterPlayer.UniqueMultiplayerID);
        }

        // A null player ID sends to every other player
        private void SendMessage<TMessage>(TMessage message, string messageType, long? playerId)
        {
            try
            {
                long[] playerIds = playerId.HasValue ? new[] { playerId.Value } : null;
                helper.Multiplayer.SendMessage(message, messageType, new[] { CropTraits.manifest.UniqueID }, playerIds);
            }
            catch (Exception exception)
            {
                monitor.Log($"Failed to send the {messageType} message: {exception}", LogLevel.Warn);
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
