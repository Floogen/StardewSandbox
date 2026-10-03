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
        public const int SLOT_AREA_HEIGHT = 224;
        public const int SLOT_SPACING = 128;
        public const int OK_BUTTON_SPRITE_INDEX = 46;
        public const int SLOT_SPRITE_INDEX = 10;
        public const float GHOST_ITEM_TRANSPARENCY = 0.4f;
        public const float GHOST_ITEM_LAYER_DEPTH = 0.865f;

        private readonly Object _machine;
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

        public SeedSplicerMenu(Object machine) : base(0, 0, 880, 640, true)
        {
            _machine = machine;
            _wasOutputReady = SpliceHelper.IsReady(_machine);

            Layout();
            UpdateStatus();
        }

        public void Layout()
        {
            Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
            xPositionOnScreen = (int)topLeft.X;
            yPositionOnScreen = (int)topLeft.Y;
            initializeUpperRightCloseButton();

            int centerX = xPositionOnScreen + (width / 2);
            int slotY = yPositionOnScreen + spaceToClearTopBorder + 64;

            // Inputs, the splice button and the output slot sit in one centered row
            int firstX = centerX - (SLOT_SPACING * 3 / 2) - (SLOT_SIZE / 2);

            _firstSlot = new ClickableComponent(new Rectangle(firstX, slotY, SLOT_SIZE, SLOT_SIZE), "FirstSlot");
            _secondSlot = new ClickableComponent(new Rectangle(firstX + SLOT_SPACING, slotY, SLOT_SIZE, SLOT_SIZE), "SecondSlot");
            _spliceButton = new ClickableTextureComponent("Splice", new Rectangle(firstX + (SLOT_SPACING * 2), slotY, SLOT_SIZE, SLOT_SIZE), null, TranslationHelper.Get("menu.button.splice"), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, OK_BUTTON_SPRITE_INDEX), 1f);
            _outputSlot = new ClickableComponent(new Rectangle(firstX + (SLOT_SPACING * 3), slotY, SLOT_SIZE, SLOT_SIZE), "OutputSlot");

            int inventoryX = xPositionOnScreen + spaceToClearSideBorder + borderWidth;
            int inventoryY = yPositionOnScreen + spaceToClearTopBorder + SLOT_AREA_HEIGHT + 64;
            // Not flagged as the player inventory, matching the forge and tailoring menus, so the hotbar numbers aren't drawn above the top row
            _inventory = new InventoryMenu(inventoryX, inventoryY, false, null, CanHighlight);

            // Locked backpack slots are only greyed out automatically for the player inventory, so they're requested explicitly
            _inventory.showGrayedOutSlots = true;
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
            _spliceButton.tryHover(x, y);

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

        // A faded copy of the placed crop hints at what the empty slot needs, since only the same crop type is accepted
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
                _statusText = hasEligibleItem ? TranslationHelper.Get("menu.status.empty") : TranslationHelper.Get("menu.status.no_eligible", new { count = BlendManager.REQUIRED_STACK_PER_SLOT });
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
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

            int centerX = xPositionOnScreen + (width / 2);
            DrawCenteredText(b, TranslationHelper.Get("menu.title"), Game1.dialogueFont, centerX, yPositionOnScreen + spaceToClearTopBorder + 8);

            DrawSlot(b, _firstSlot, _firstItem, _ghostItem);
            DrawSlot(b, _secondSlot, _secondItem, _ghostItem);
            DrawCenteredText(b, "+", Game1.dialogueFont, (_firstSlot.bounds.Right + _secondSlot.bounds.Left) / 2, _firstSlot.bounds.Y + 8);

            DrawOutputSlot(b);

            bool canSplice = SpliceHelper.HasOutput(_machine) is false && CropTraits.blendManager.CanBlend(_firstItem, _secondItem);
            _spliceButton.draw(b, canSplice ? Color.White : Color.Gray * 0.5f, 0.88f);

            string wrappedStatus = Game1.parseText(_statusText, Game1.smallFont, width - 128);
            DrawCenteredText(b, wrappedStatus, Game1.smallFont, centerX, _firstSlot.bounds.Bottom + 24);

            _inventory.draw(b);
            base.draw(b);

            if (_hoveredItem is not null)
            {
                drawToolTip(b, _hoveredItem.getDescription(), _hoveredItem.DisplayName, _hoveredItem);
            }
            else if (_spliceButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(b, _spliceButton.hoverText, Game1.smallFont);
            }

            drawMouse(b);
        }

        public void DrawSlot(SpriteBatch b, ClickableComponent slot, Item item, Item ghostItem)
        {
            Vector2 position = new Vector2(slot.bounds.X, slot.bounds.Y);
            b.Draw(Game1.menuTexture, position, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, SLOT_SPRITE_INDEX), Color.White);

            if (item is not null)
            {
                item.drawInMenu(b, position, 1f);
                return;
            }

            // Empty slots show the ghost when the other slot is filled, without a stack number or shadow
            ghostItem?.drawInMenu(b, position, 1f, GHOST_ITEM_TRANSPARENCY, GHOST_ITEM_LAYER_DEPTH, StackDrawType.Hide, Color.Gray, false);
        }

        // A running splice shows its seeds faded, and they become collectable once it finishes
        public void DrawOutputSlot(SpriteBatch b)
        {
            Vector2 position = new Vector2(_outputSlot.bounds.X, _outputSlot.bounds.Y);
            b.Draw(Game1.menuTexture, position, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, SLOT_SPRITE_INDEX), Color.White);

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

        public void DrawCenteredText(SpriteBatch b, string text, SpriteFont font, int centerX, int y)
        {
            Vector2 size = font.MeasureString(text);
            Utility.drawTextWithShadow(b, text, font, new Vector2(centerX - (size.X / 2), y), Game1.textColor);
        }
    }
}
