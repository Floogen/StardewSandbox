using FishingLogbook.Framework.Models.Stickers;
using FishingLogbook.Framework.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FishingLogbook.Framework.Managers
{
    public class StickerManager : BaseManager
    {
        // this needs to match the UniqueID in manifest.json
        private const string STICKER_ASSET_PREFIX = "Mods/PeacefulEnd.FishingLogbook/Stickers";

        private readonly Color _outlineColor = new Color(255, 245, 232, 255);
        private const int OUTLINE_THICKNESS = 1;

        private readonly Dictionary<string, StickerData> _stickersByAssetName = new Dictionary<string, StickerData>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, StickerData> _stickersByItemId = new Dictionary<string, StickerData>(StringComparer.OrdinalIgnoreCase);

        public StickerManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // the catalog is built on save load rather than launch, since conditional Content Patcher patches are not applied yet at launch
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (_stickersByAssetName.TryGetValue(e.NameWithoutLocale.Name, out StickerData sticker) is false)
            {
                return;
            }

            // Low priority lets other mods replace a generated sticker with their own artwork via an Action: Load patch
            e.LoadFrom(() => LoadStickerTexture(sticker), AssetLoadPriority.Low);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            BuildCatalog();
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            _stickersByAssetName.Clear();
            _stickersByItemId.Clear();
        }

        /// <summary>
        /// Gets the asset name a sticker for the given item is served under.
        /// </summary>
        public string GetStickerPath(string qualifiedItemId)
        {
            // separators would change the asset name from the one that was registered, so they get folded away
            string safeItemId = qualifiedItemId.Replace('/', '_').Replace('\\', '_');

            return $"{STICKER_ASSET_PREFIX}/{safeItemId}";
        }

        /// <summary>
        /// Gets every sticker available to the given player, sorted so the sequence does not depend on catalog build order.
        /// </summary>
        /// <param name="who">The player whose catch record to filter against, or null for the local player</param>
        /// <param name="skipCaughtRequirement">Whether to include fish the player has not caught yet</param>
        public List<StickerData> GetAvailableStickers(Farmer who, bool skipCaughtRequirement = false)
        {
            return _stickersByItemId.Values.Where(s => skipCaughtRequirement is true || who.fishCaught.ContainsKey(s.Id)).OrderBy(s => s.Id, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Gets a random selection of stickers, rerolled once per in-game day.
        /// </summary>
        /// <param name="count">The number of stickers to return, or null for every available sticker</param>
        /// <param name="salt">An extra value to vary the selection between separate pages</param>
        /// <param name="who">The player whose catch record to filter against, or null for the local player</param>
        /// <param name="skipCaughtRequirement">Whether to include fish the player has not caught yet</param>
        public List<StickerData> GetRandomStickers(Farmer who, int? count = null, int salt = 0, bool skipCaughtRequirement = false)
        {
            // the filter runs before the take, otherwise the requested count is silently reduced by whatever it removes
            List<StickerData> available = GetAvailableStickers(who, skipCaughtRequirement);

            Utility.Shuffle(StickerHelper.CreateDayRandom(salt), available);

            if (count is null)
            {
                return available;
            }

            return available.Take(count.Value).ToList();
        }

        /// <summary>
        /// Generates the pixel data for the given stickers ahead of time, reading each source sprite sheet only once.
        /// Call this before a batch of stickers is drawn, so the readbacks do not land mid-render.
        /// </summary>
        public void Prepare(IEnumerable<StickerData> stickers)
        {
            GenerateStickers(stickers.Where(s => s.IsGenerated is false));
        }

        /// <summary>
        /// Builds the sticker catalog. This records where each sticker comes from, but does not generate any pixel data.
        /// </summary>
        private void BuildCatalog()
        {
            _stickersByAssetName.Clear();
            _stickersByItemId.Clear();

            foreach (var fishEntry in FishDataHelper.GetAllFish())
            {
                string assetName = GetStickerPath(fishEntry.QualifiedItemId);

                StickerData sticker = new StickerData()
                {
                    Id = fishEntry.QualifiedItemId,
                    TexturePath = assetName,
                    SourceAssetName = fishEntry.ItemData.TextureName,
                    SheetSourceRectangle = fishEntry.ItemData.GetSourceRect(),
                    OutlineColor = _outlineColor,
                    OutlineThickness = OUTLINE_THICKNESS,
                    IncludeDiagonals = false
                };

                var validation = sticker.IsValid();
                if (validation.Result is false)
                {
                    monitor.Log($"Skipped the sticker for {fishEntry.QualifiedItemId}: {validation.Error}", LogLevel.Trace);
                    continue;
                }

                _stickersByAssetName[assetName] = sticker;
                _stickersByItemId[sticker.Id] = sticker;
            }

            monitor.Log($"Cataloged {_stickersByAssetName.Count} stickers", LogLevel.Trace);
        }

        /// <summary>
        /// Generates the pixel data for the given stickers, grouped so each source sprite sheet is read back only once.
        /// </summary>
        private void GenerateStickers(IEnumerable<StickerData> stickers)
        {
            foreach (var group in stickers.GroupBy(s => s.SourceAssetName, StringComparer.OrdinalIgnoreCase))
            {
                Color[] sheetPixels;
                int sheetWidth, sheetHeight;

                try
                {
                    // GameContent (rather than ModContent) is used so Content Patcher edits to the source sheet are reflected
                    Texture2D sheet = helper.GameContent.Load<Texture2D>(group.Key);

                    sheetWidth = sheet.Width;
                    sheetHeight = sheet.Height;
                    sheetPixels = new Color[sheetWidth * sheetHeight];

                    // this stalls the GPU pipeline, which is why it happens once per sheet instead of once per sticker
                    sheet.GetData(sheetPixels);
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed to read the sprite sheet [{group.Key}], so its stickers will not be generated: {ex}", LogLevel.Warn);
                    continue;
                }

                foreach (StickerData sticker in group)
                {
                    var generation = StickerHelper.TryCreateOutlinedPixels(sheetPixels, sheetWidth, sheetHeight, sticker.SheetSourceRectangle, sticker.OutlineColor, sticker.OutlineThickness, sticker.IncludeDiagonals, sticker.AlphaThreshold, out Color[] pixels, out Point size);
                    if (generation.Result is false)
                    {
                        monitor.Log($"Failed to generate the sticker for {sticker.Id}: {generation.Error}", LogLevel.Warn);
                        continue;
                    }

                    if (size != sticker.Size)
                    {
                        monitor.Log($"The sticker for {sticker.Id} generated at {size}, but {sticker.Size} was expected", LogLevel.Warn);
                        continue;
                    }

                    sticker.SetPixels(pixels);
                }
            }
        }

        /// <summary>
        /// Generates the given sticker if needed, then builds a texture from it.
        /// </summary>
        private Texture2D LoadStickerTexture(StickerData sticker)
        {
            if (sticker.IsGenerated is false)
            {
                GenerateStickers(new[] { sticker });
            }

            Texture2D texture = sticker.CreateTexture();
            if (texture is not null)
            {
                return texture;
            }

            // an asset loader cannot return null, so a placeholder stands in for a sticker which failed to generate
            monitor.Log($"Serving a blank placeholder for the sticker [{sticker.TexturePath}], as its pixel data could not be generated", LogLevel.Warn);

            // the placeholder keeps the expected dimensions, so callers drawing against Bounds stay in range
            Point size = sticker.Size;

            return new Texture2D(Game1.graphics.GraphicsDevice, Math.Max(size.X, 1), Math.Max(size.Y, 1));
        }
    }
}
