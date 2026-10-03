using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace CropTraits.Framework.UI.Menus
{
    public class SeedSplicerMenu : IClickableMenu
    {
        public const int SLOT_SIZE = 64;
        public const int MENU_SCALE = 4;
        public const int SECTION_GAP = 16;
        public const int BOX_PADDING = 32;
        public const int INVENTORY_COLUMNS = 12;
        public const int INVENTORY_ROWS = 3;
        public const float ART_LAYER_DEPTH = 0.86f;
        public const float GHOST_ITEM_TRANSPARENCY = 0.4f;
        public const float GHOST_ITEM_LAYER_DEPTH = 0.865f;
        public const float REQUIRED_COUNT_LAYER_DEPTH = 0.866f;
        public const float REQUIRED_COUNT_SCALE = 3f;
        public const float BUTTON_LAYER_DEPTH = 0.88f;
        public const double SPLICE_BUTTON_SWAP_INTERVAL_MILLISECONDS = 2000;
        public const int SHAKE_INTENSITY = 2;
        public const double SHAKE_INTERVAL_MILLISECONDS = 50;

        // Pixel positions in the spritesheet (before scaling), so these must match the art
        public static readonly Rectangle ART_SOURCE = new Rectangle(0, 0, 116, 85);
        public static readonly Point FIRST_SLOT_PIXEL = new Point(30, 26); // Top left of the slot's inner 16x16 area
        public static readonly Point SECOND_SLOT_PIXEL = new Point(67, 26);
        public static readonly Point OUTPUT_SLOT_PIXEL = new Point(49, 53);
        public static readonly Point SPLICE_BUTTON_PIXEL = new Point(88, 56);
        public static readonly Rectangle SPLICE_BUTTON_IDLE_SOURCE = new Rectangle(144, 16, 13, 13);
        public static readonly Rectangle SPLICE_BUTTON_READY_SOURCE = new Rectangle(128, 16, 13, 13);
        public static readonly Rectangle SPLICE_BUTTON_ACTIVE_OFF_SOURCE = new Rectangle(144, 0, 13, 13);
        public static readonly Rectangle SPLICE_BUTTON_ACTIVE_ON_SOURCE = new Rectangle(128, 0, 13, 13);

        private readonly Object _machine;
        private readonly Texture2D _menuTexture;
        private readonly System.Random _shakeRandom = new System.Random();
        private Vector2 _shakeOffset;
        private double _shakeTimer;
        private Vector2 _artPosition;
        private Rectangle _textBoxBounds;
        private Rectangle _inventoryBoxBounds;
        private InventoryMenu _inventory;
        private ClickableComponent _firstSlot;
        private ClickableComponent _secondSlot;
        private ClickableComponent _outputSlot;
        private ClickableTextureComponent _spliceButton;
        private Item _firstItem;
        private Item _secondItem;
        private Item _hoveredItem;
        private Item _ghostItem;
        private string _statusText;
        private bool _wasOutputReady;
        private int _lastProgressPercent;

        public SeedSplicerMenu(Object machine) : base(0, 0, GetMenuWidth(), GetMenuHeight(), true)
        {
            _machine = machine;
            _menuTexture = Game1.content.Load<Texture2D>(AssetManager.SEED_SPLICER_MENU_TEXTURE_PATH);
            _wasOutputReady = SpliceHelper.IsReady(_machine);

            Layout();
            UpdateStatus();
        }

        public static int GetMenuWidth()
        {
            return (INVENTORY_COLUMNS * SLOT_SIZE) + (BOX_PADDING * 2);
        }

        public static int GetMenuHeight()
        {
            return (ART_SOURCE.Height * MENU_SCALE) + SECTION_GAP + (INVENTORY_ROWS * SLOT_SIZE) + (BOX_PADDING * 2);
        }

        public void Layout()
        {
            Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
            xPositionOnScreen = (int)topLeft.X;
            yPositionOnScreen = (int)topLeft.Y;
            initializeUpperRightCloseButton();

            int artWidth = ART_SOURCE.Width * MENU_SCALE;
            int artHeight = ART_SOURCE.Height * MENU_SCALE;
            _artPosition = new Vector2(xPositionOnScreen, yPositionOnScreen);
            _textBoxBounds = new Rectangle(xPositionOnScreen + artWidth + SECTION_GAP, yPositionOnScreen, width - artWidth - SECTION_GAP, artHeight);
            _inventoryBoxBounds = new Rectangle(xPositionOnScreen, yPositionOnScreen + artHeight + SECTION_GAP, width, height - artHeight - SECTION_GAP);

            _firstSlot = new ClickableComponent(GetBounds(FIRST_SLOT_PIXEL, SLOT_SIZE, SLOT_SIZE), "FirstSlot");
            _secondSlot = new ClickableComponent(GetBounds(SECOND_SLOT_PIXEL, SLOT_SIZE, SLOT_SIZE), "SecondSlot");
            _outputSlot = new ClickableComponent(GetBounds(OUTPUT_SLOT_PIXEL, SLOT_SIZE, SLOT_SIZE), "OutputSlot");

            Rectangle buttonBounds = GetBounds(SPLICE_BUTTON_PIXEL, SPLICE_BUTTON_IDLE_SOURCE.Width * MENU_SCALE, SPLICE_BUTTON_IDLE_SOURCE.Height * MENU_SCALE);
            _spliceButton = new ClickableTextureComponent("Splice", buttonBounds, null, TranslationHelper.Get("menu.button.splice"), _menuTexture, SPLICE_BUTTON_IDLE_SOURCE, MENU_SCALE);

            int inventoryX = _inventoryBoxBounds.X + BOX_PADDING;
            int inventoryY = _inventoryBoxBounds.Y + BOX_PADDING;

            _inventory = new InventoryMenu(inventoryX, inventoryY, false, null, CanHighlight);
            _inventory.showGrayedOutSlots = true;
        }

        // Converts a pixel position in the art into screen bounds
        public Rectangle GetBounds(Point pixel, int boundsWidth, int boundsHeight)
        {
            return new Rectangle((int)_artPosition.X + (pixel.X * MENU_SCALE), (int)_artPosition.Y + (pixel.Y * MENU_SCALE), boundsWidth, boundsHeight);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            Layout();
        }

        public bool CanHighlight(Item item)
        {
            // Nothing new can go in until the current splice is collected
            if (SpliceHelper.HasOutput(_machine) || (_firstItem is not null && _secondItem is not null))
            {
                return false;
            }

            return CropTraits.blendManager.CanPlace(item, _firstItem ?? _secondItem);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (_outputSlot.containsPoint(x, y))
            {
                TryCollectOutput();
                return;
            }

            if (_firstSlot.containsPoint(x, y))
            {
                _firstItem = ReturnToInventory(_firstItem);
                UpdateStatus();
                return;
            }

            if (_secondSlot.containsPoint(x, y))
            {
                _secondItem = ReturnToInventory(_secondItem);
                UpdateStatus();
                return;
            }

            if (_spliceButton.containsPoint(x, y))
            {
                TrySplice();
                return;
            }

            int inventoryIndex = _inventory.getInventoryPositionOfClick(x, y);
            if (inventoryIndex < 0 || inventoryIndex >= _inventory.actualInventory.Count)
            {
                return;
            }

            Item candidate = _inventory.actualInventory[inventoryIndex];
            if (candidate is null)
            {
                return;
            }

            if (CanHighlight(candidate) is false)
            {
                Game1.playSound("cancel");

                bool hasOpenSlot = SpliceHelper.HasOutput(_machine) is false && (_firstItem is null || _secondItem is null);
                if (hasOpenSlot && CropTraits.blendManager.IsOnlyShortOfStack(candidate, _firstItem ?? _secondItem))
                {
                    _statusText = TranslationHelper.Get("menu.status.short_stack", new { count = BlendManager.REQUIRED_STACK_PER_SLOT, name = candidate.DisplayName, held = candidate.Stack });
                }

                return;
            }

            // Only the required amount is taken, so the rest of the stack stays in the inventory
            Item takenItem = candidate.getOne();
            takenItem.Stack = BlendManager.REQUIRED_STACK_PER_SLOT;
            _inventory.actualInventory[inventoryIndex] = candidate.ConsumeStack(BlendManager.REQUIRED_STACK_PER_SLOT);

            if (_firstItem is null)
            {
                _firstItem = takenItem;
            }
            else
            {
                _secondItem = takenItem;
            }

            Game1.playSound("stoneStep");
            UpdateStatus();
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            _hoveredItem = null;

            if (SpliceHelper.IsSplicing(_machine) is false)
            {
                _spliceButton.tryHover(x, y);
            }

            _inventory.hover(x, y, null);

            Item inventoryItem = _inventory.getItemAt(x, y);
            if (inventoryItem is not null)
            {
                _hoveredItem = inventoryItem;
            }
            else if (_firstSlot.containsPoint(x, y))
            {
                _hoveredItem = _firstItem;
            }
            else if (_secondSlot.containsPoint(x, y))
            {
                _hoveredItem = _secondItem;
            }
            else if (_outputSlot.containsPoint(x, y) && SpliceHelper.IsReady(_machine))
            {
                // The result stays hidden until the splice finishes
                _hoveredItem = _machine.heldObject.Value;
            }
        }

        // Time keeps running in multiplayer while the menu is open, so the status follows the splice's progress
        public override void update(GameTime time)
        {
            base.update(time);
            UpdateShake(time);

            bool isOutputReady = SpliceHelper.IsReady(_machine);
            if (isOutputReady != _wasOutputReady)
            {
                _wasOutputReady = isOutputReady;
                UpdateStatus();
                return;
            }

            if (SpliceHelper.IsSplicing(_machine) is false)
            {
                return;
            }

            int progressPercent = SpliceHelper.GetProgressPercent(_machine);
            if (progressPercent != _lastProgressPercent)
            {
                _lastProgressPercent = progressPercent;
                UpdateStatus();
            }
        }

        public void UpdateShake(GameTime time)
        {
            if (SpliceHelper.IsSplicing(_machine) is false)
            {
                _shakeOffset = Vector2.Zero;
                _shakeTimer = 0;
                return;
            }

            _shakeTimer += time.ElapsedGameTime.TotalMilliseconds;
            if (_shakeTimer < SHAKE_INTERVAL_MILLISECONDS)
            {
                return;
            }

            // A long frame only produces one new offset rather than catching up
            _shakeTimer %= SHAKE_INTERVAL_MILLISECONDS;
            _shakeOffset = new Vector2(_shakeRandom.Next(-SHAKE_INTENSITY, SHAKE_INTENSITY + 1), _shakeRandom.Next(-SHAKE_INTENSITY, SHAKE_INTENSITY + 1));
        }

        // The result is decided now, but the seeds wait in the machine until the splice finishes
        public void TrySplice()
        {
            if (SpliceHelper.HasOutput(_machine) || CropTraits.blendManager.CanBlend(_firstItem, _secondItem) is false)
            {
                Game1.playSound("cancel");
                return;
            }

            string seedId = CropTraits.blendManager.GetSeedId(_firstItem);
            BlendOutcome outcome = CropTraits.blendManager.Blend(seedId, _firstItem, _secondItem);

            // Each slot only ever holds the required amount, so both are used up
            _firstItem = null;
            _secondItem = null;

            if (outcome.Seeds is not Object seeds)
            {
                CropTraits.monitor.Log($"Splice output {outcome.Seeds?.QualifiedItemId} isn't an object, so it was given directly instead of held in the machine.", LogLevel.Warn);
                DropRemainder(Game1.player.addItemToInventory(outcome.Seeds));
                UpdateStatus();
                return;
            }

            SpliceHelper.StartSplice(_machine, seeds, outcome.UpgradedTraits);
            _wasOutputReady = false;

            Game1.playSound("Ship");
            UpdateStatus();
        }

        public void TryCollectOutput()
        {
            if (SpliceHelper.HasOutput(_machine) is false)
            {
                return;
            }

            if (SpliceHelper.IsReady(_machine) is false)
            {
                Game1.playSound("cancel");
                return;
            }

            // Captured before adding, since the seeds may merge into an existing stack
            Object seeds = _machine.heldObject.Value;
            int seedCount = seeds.Stack;
            string seedName = seeds.DisplayName;
            TraitSet upgradedTraits = SpliceHelper.GetUpgradedTraits(_machine);

            Item remainder = Game1.player.addItemToInventory(seeds);
            if (remainder is not null)
            {
                _machine.heldObject.Value = remainder as Object;
                Game1.playSound("cancel");
                _statusText = TranslationHelper.Get("menu.status.inventory_full");
                return;
            }

            SpliceHelper.ClearSplice(_machine);
            _wasOutputReady = false;
            UpdateStatus();

            _statusText = TranslationHelper.Get("menu.status.created", new { count = seedCount, name = seedName });

            if (upgradedTraits.Count == 0)
            {
                Game1.playSound("coin");
                return;
            }

            // Upgrades are rare and easy to miss in the tooltip, so they get their own line and sound
            string upgradedKey = upgradedTraits.Count == 1 ? "menu.status.upgraded" : "menu.status.upgraded_multiple";
            string upgradedLine = TranslationHelper.Get(upgradedKey, new { traits = CropTraits.traitManager.GetLabel(upgradedTraits.ToKey()) });

            Game1.playSound("reward");
            _statusText = $"{_statusText}{System.Environment.NewLine}{upgradedLine}";
        }

        // Returns whatever didn't fit, so it stays in the slot
        public Item ReturnToInventory(Item item)
        {
            if (item is null)
            {
                return null;
            }

            Game1.playSound("dwop");
            return Game1.player.addItemToInventory(item);
        }

        // A faded copy of the placed crop to hint at what the empty slot needs, since only the same crop type is accepted
        public void UpdateGhostItem()
        {
            Item placedItem = _firstItem ?? _secondItem;
            if (placedItem is null || (_firstItem is not null && _secondItem is not null))
            {
                _ghostItem = null;
                return;
            }

            // getOne keeps the crop's color (such as flowers), and quality is cleared since any quality is accepted
            _ghostItem = placedItem.getOne();
            _ghostItem.Quality = Object.lowQuality;
        }

        public void UpdateStatus()
        {
            UpdateGhostItem();

            if (SpliceHelper.HasOutput(_machine))
            {
                _statusText = GetMachineStatus();
            }
            else if (_firstItem is null && _secondItem is null)
            {
                bool hasEligibleItem = CropTraits.blendManager.HasEligibleItem(Game1.player.Items);
                _statusText = hasEligibleItem ? TranslationHelper.Get("menu.status.empty", new { count = BlendManager.REQUIRED_STACK_PER_SLOT }) : TranslationHelper.Get("menu.status.no_eligible", new { count = BlendManager.REQUIRED_STACK_PER_SLOT });
            }
            else if (_firstItem is null || _secondItem is null)
            {
                _statusText = TranslationHelper.Get("menu.status.needs_second");
            }
            else if (CropTraits.blendManager.HasTraits(_firstItem) is false && CropTraits.blendManager.HasTraits(_secondItem) is false)
            {
                _statusText = TranslationHelper.Get("menu.status.needs_traits");
            }
            else if (CropTraits.blendManager.HasEnoughForSlot(_firstItem) is false || CropTraits.blendManager.HasEnoughForSlot(_secondItem) is false)
            {
                _statusText = TranslationHelper.Get("menu.status.needs_stack", new { count = BlendManager.REQUIRED_STACK_PER_SLOT });
            }
            else if (CropTraits.blendManager.IsFullyMaxedIdenticalPair(_firstItem, _secondItem))
            {
                _statusText = TranslationHelper.Get("menu.status.max_tier");
            }
            else if (CropTraits.blendManager.IsIdenticalPair(_firstItem, _secondItem))
            {
                // A tier upgrade is the only thing an identical pair can gain, so its odds are shown like dilution's
                int upgradeChance = (int)System.Math.Round(CropTraits.blendManager.GetAnyTierUpgradeChance(_firstItem, _secondItem) * 100);
                _statusText = TranslationHelper.Get("menu.status.ready_crossbreed_identical", new { chance = upgradeChance });
            }
            else if (CropTraits.blendManager.HasTraits(_firstItem) && CropTraits.blendManager.HasTraits(_secondItem))
            {
                _statusText = TranslationHelper.Get("menu.status.ready_crossbreed");
            }
            else
            {
                _statusText = GetDiluteStatus();
            }
        }

        public string GetMachineStatus()
        {
            if (SpliceHelper.IsReady(_machine))
            {
                return TranslationHelper.Get("menu.status.ready_collect");
            }

            return TranslationHelper.Get("menu.status.splicing", new { percent = SpliceHelper.GetProgressPercent(_machine) });
        }

        // The split chance depends on the traited parent's quality, so showing it is the only way players see that quality matters
        public string GetDiluteStatus()
        {
            Item traitedItem = CropTraits.blendManager.HasTraits(_firstItem) ? _firstItem : _secondItem;

            // Removing the only trait is the same as a wipe, so a chance would be misleading
            if (TraitModDataHelper.GetTraits(traitedItem.modData).Count <= 1)
            {
                return TranslationHelper.Get("menu.status.ready_dilute_single");
            }

            int splitChance = (int)System.Math.Round(DilutionHelper.GetSplitChance(traitedItem.Quality) * 100);
            return TranslationHelper.Get("menu.status.ready_dilute", new { chance = splitChance });
        }

        public void ReturnAllItems()
        {
            _firstItem = DropRemainder(ReturnToInventory(_firstItem));
            _secondItem = DropRemainder(ReturnToInventory(_secondItem));
        }

        public Item DropRemainder(Item item)
        {
            if (item is not null)
            {
                Game1.createItemDebris(item, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
            }

            return null;
        }

        protected override void cleanupBeforeExit()
        {
            ReturnAllItems();
            base.cleanupBeforeExit();
        }

        public override void emergencyShutDown()
        {
            ReturnAllItems();
            base.emergencyShutDown();
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

            b.Draw(_menuTexture, _artPosition + _shakeOffset, ART_SOURCE, Color.White, 0f, Vector2.Zero, MENU_SCALE, SpriteEffects.None, ART_LAYER_DEPTH);
            drawTextureBox(b, _textBoxBounds.X, _textBoxBounds.Y, _textBoxBounds.Width, _textBoxBounds.Height, Color.White);
            drawTextureBox(b, _inventoryBoxBounds.X, _inventoryBoxBounds.Y, _inventoryBoxBounds.Width, _inventoryBoxBounds.Height, Color.White);

            DrawTextBox(b);

            DrawSlot(b, _firstSlot, _firstItem, _ghostItem);
            DrawSlot(b, _secondSlot, _secondItem, _ghostItem);
            DrawOutputSlot(b);

            bool canSplice = SpliceHelper.HasOutput(_machine) is false && CropTraits.blendManager.CanBlend(_firstItem, _secondItem);
            _spliceButton.sourceRect = SPLICE_BUTTON_IDLE_SOURCE;
            if (canSplice)
            {
                bool isShowingAlternateSprite = (long)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / SPLICE_BUTTON_SWAP_INTERVAL_MILLISECONDS) % 2 == 1;
                _spliceButton.sourceRect = isShowingAlternateSprite ? SPLICE_BUTTON_READY_SOURCE : SPLICE_BUTTON_IDLE_SOURCE;
            }
            else if (SpliceHelper.IsSplicing(_machine))
            {
                _spliceButton.sourceRect = SPLICE_BUTTON_ACTIVE_ON_SOURCE;
            }
            _spliceButton.draw(b, Color.White, BUTTON_LAYER_DEPTH, 0, (int)_shakeOffset.X, (int)_shakeOffset.Y);

            _inventory.draw(b);
            base.draw(b);

            if (_hoveredItem is not null)
            {
                drawToolTip(b, _hoveredItem.getDescription(), _hoveredItem.DisplayName, _hoveredItem);
            }

            drawMouse(b);
        }

        public void DrawTextBox(SpriteBatch b)
        {
            int textX = _textBoxBounds.X + BOX_PADDING;
            int textY = _textBoxBounds.Y + BOX_PADDING;
            int textWidth = _textBoxBounds.Width - (BOX_PADDING * 2);

            string title = TranslationHelper.Get("menu.title");
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont, new Vector2(textX, textY), Game1.textColor);

            int statusY = textY + (int)Game1.dialogueFont.MeasureString(title).Y + 8;
            Utility.drawTextWithShadow(b, Game1.parseText(_statusText, Game1.smallFont, textWidth), Game1.smallFont, new Vector2(textX, statusY), Game1.textColor);
        }

        public void DrawSlot(SpriteBatch b, ClickableComponent slot, Item item, Item ghostItem)
        {
            // The slot frame is part of the art, so only the contents are drawn (moving with the art when it shakes)
            Vector2 position = new Vector2(slot.bounds.X, slot.bounds.Y) + _shakeOffset;

            if (item is not null)
            {
                item.drawInMenu(b, position, 1f);
                return;
            }

            // Empty slots show the ghost when the other slot is filled, without its own stack number or shadow
            ghostItem?.drawInMenu(b, position, 1f, GHOST_ITEM_TRANSPARENCY, GHOST_ITEM_LAYER_DEPTH, StackDrawType.Hide, Color.Gray, false);

            if (SpliceHelper.HasOutput(_machine) is false)
            {
                DrawRequiredCount(b, position);
            }
        }

        public void DrawRequiredCount(SpriteBatch b, Vector2 slotPosition)
        {
            int requiredCount = BlendManager.REQUIRED_STACK_PER_SLOT;
            float digitsWidth = Utility.getWidthOfTinyDigitString(requiredCount, REQUIRED_COUNT_SCALE);

            Vector2 digitsPosition = slotPosition + new Vector2(SLOT_SIZE - digitsWidth + REQUIRED_COUNT_SCALE, SLOT_SIZE - (6f * REQUIRED_COUNT_SCALE) + 1f);
            Utility.drawTinyDigits(requiredCount, b, digitsPosition, REQUIRED_COUNT_SCALE, REQUIRED_COUNT_LAYER_DEPTH, Color.White * GHOST_ITEM_TRANSPARENCY);
        }

        // A running splice shows its seeds faded, and they become collectable once it finishes
        public void DrawOutputSlot(SpriteBatch b)
        {
            Vector2 position = new Vector2(_outputSlot.bounds.X, _outputSlot.bounds.Y) + _shakeOffset;

            Object seeds = _machine.heldObject.Value;
            if (seeds is null)
            {
                return;
            }

            if (SpliceHelper.IsReady(_machine))
            {
                seeds.drawInMenu(b, position, 1f);
                return;
            }

            seeds.drawInMenu(b, position, 1f, GHOST_ITEM_TRANSPARENCY, GHOST_ITEM_LAYER_DEPTH, StackDrawType.Hide, Color.Gray, false);
        }
    }
}
