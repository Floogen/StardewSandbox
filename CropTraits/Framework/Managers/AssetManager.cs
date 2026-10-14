using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using System.Collections.Generic;

namespace CropTraits.Framework.Managers
{
    public class AssetManager : BaseManager
    {
        public const string BIG_CRAFTABLES_DATA_PATH = "Data/BigCraftables";
        public const string OBJECTS_DATA_PATH = "Data/Objects";
        public const string SHOPS_DATA_PATH = "Data/Shops";
        public const string CRAFTING_RECIPES_DATA_PATH = "Data/CraftingRecipes";
        public const string MAIL_DATA_PATH = "Data/Mail";

        public const string PIERRE_SHOP_ID = "SeedShop";
        public const int MUTAGENIC_FERTILIZER_FARMING_LEVEL = 5;
        public static readonly string MUTAGENIC_FERTILIZER_SHOP_CONDITION = $"PLAYER_FARMING_LEVEL Current {MUTAGENIC_FERTILIZER_FARMING_LEVEL}";

        public const string SPRITE_SHEET_TEXTURE_PATH = "Mods/PeacefulEnd.CropTraits/Spritesheet";
        public const string SPRITE_SHEET_FILE_PATH = "Framework/Assets/Spritesheet.png";
        public const string SEED_SPLICER_MENU_TEXTURE_PATH = "Mods/PeacefulEnd.CropTraits/SeedSplicerMenu";
        public const string SEED_SPLICER_MENU_FILE_PATH = "Framework/Assets/SeedSplicerMenu.png";

        // Positions in the spritesheet
        public static readonly Rectangle MUTAGENIC_FERTILIZER_DIRT_SOURCE = new Rectangle(32, 16, 16, 16);
        public static readonly Rectangle MUTATION_BADGE_SOURCE = new Rectangle(16, 9, 7, 7);

        public const int SEED_SPLICER_PRICE = 500;
        public const int MUTAGENIC_FERTILIZER_PRICE = 50;
        public const int MUTAGENIC_FERTILIZER_SHOP_PRICE = 150;
        public const int FIELD_NOTES_SHOP_PRICE = 0;
        public const int FERTILIZER_CATEGORY = -19;

        public const string PARCHMENT_BOOK_ID_FIELD = "PeacefulEnd.Parchment/CustomFields/BookId";

        public AssetManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(SPRITE_SHEET_TEXTURE_PATH))
            {
                e.LoadFromModFile<Texture2D>(SPRITE_SHEET_FILE_PATH, AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(SEED_SPLICER_MENU_TEXTURE_PATH))
            {
                e.LoadFromModFile<Texture2D>(SEED_SPLICER_MENU_FILE_PATH, AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(BIG_CRAFTABLES_DATA_PATH))
            {
                e.Edit(asset =>
                {
                    IDictionary<string, BigCraftableData> data = asset.AsDictionary<string, BigCraftableData>().Data;
                    data[CropTraits.SEED_SPLICER_ID] = new BigCraftableData
                    {
                        Name = CropTraits.SEED_SPLICER_ID,
                        DisplayName = TranslationHelper.Get("item.seed_splicer.name"),
                        Description = TranslationHelper.Get("item.seed_splicer.description"),
                        Price = SEED_SPLICER_PRICE,
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = true,
                        Texture = SPRITE_SHEET_TEXTURE_PATH,
                        SpriteIndex = 0
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(OBJECTS_DATA_PATH))
            {
                e.Edit(asset =>
                {
                    IDictionary<string, ObjectData> data = asset.AsDictionary<string, ObjectData>().Data;
                    data[CropTraits.MUTAGENIC_FERTILIZER_ID] = new ObjectData
                    {
                        Name = CropTraits.MUTAGENIC_FERTILIZER_ID,
                        DisplayName = TranslationHelper.Get("item.mutagenic_fertilizer.name"),
                        Description = TranslationHelper.Get("item.mutagenic_fertilizer.description"),
                        Type = "Basic",
                        Category = FERTILIZER_CATEGORY,
                        Price = MUTAGENIC_FERTILIZER_PRICE,
                        Texture = SPRITE_SHEET_TEXTURE_PATH,
                        SpriteIndex = 2
                    };

                    data[CropTraits.JOURNAL_ID] = new ObjectData
                    {
                        Name = CropTraits.JOURNAL_ID,
                        DisplayName = TranslationHelper.Get("item.journal.name"),
                        Description = TranslationHelper.Get("item.journal.description"),
                        Type = "Basic",
                        Category = 0,
                        Price = 0,
                        Edibility = StardewValley.Object.inedible,
                        Texture = SPRITE_SHEET_TEXTURE_PATH,
                        SpriteIndex = 6,
                        CustomFields = new Dictionary<string, string>
                        {
                            { PARCHMENT_BOOK_ID_FIELD, CropTraits.JOURNAL_ID }
                        }
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(SHOPS_DATA_PATH))
            {
                e.Edit(asset =>
                {
                    IDictionary<string, ShopData> data = asset.AsDictionary<string, ShopData>().Data;
                    if (data.TryGetValue(PIERRE_SHOP_ID, out ShopData pierreShop) is false)
                    {
                        monitor.Log($"Couldn't find the {PIERRE_SHOP_ID} shop, so Mutagenic Fertilizer and the Field Notes won't be sold by Pierre.", LogLevel.Warn);
                        return;
                    }

                    AddMutagenicFertilizerToShop(pierreShop);
                    AddFieldNotesToShop(pierreShop);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(CRAFTING_RECIPES_DATA_PATH))
            {
                e.Edit(asset => CropTraits.recipeManager.EditCraftingRecipes(asset.AsDictionary<string, string>().Data));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(MAIL_DATA_PATH))
            {
                e.Edit(asset => CropTraits.mailManager.EditMail(asset.AsDictionary<string, string>().Data));
            }
        }

        public void OnLocaleChanged(object sender, LocaleChangedEventArgs e)
        {
            helper.GameContent.InvalidateCache(asset => asset.NameWithoutLocale.IsEquivalentTo(OBJECTS_DATA_PATH) || asset.NameWithoutLocale.IsEquivalentTo(BIG_CRAFTABLES_DATA_PATH) || asset.NameWithoutLocale.IsEquivalentTo(MAIL_DATA_PATH));
        }

        private void AddMutagenicFertilizerToShop(ShopData shop)
        {
            shop.Items ??= new List<ShopItemData>();

            // Skip if another edit (or an earlier pass) already added it
            if (shop.Items.Exists(item => item.Id == CropTraits.MUTAGENIC_FERTILIZER_ID))
            {
                return;
            }

            ShopItemData shopItem = new ShopItemData
            {
                Id = CropTraits.MUTAGENIC_FERTILIZER_ID,
                ItemId = CropTraits.MUTAGENIC_FERTILIZER_QUALIFIED_ID,
                Price = MUTAGENIC_FERTILIZER_SHOP_PRICE,
                Condition = MUTAGENIC_FERTILIZER_SHOP_CONDITION
            };

            // Place it after the last fertilizer so it sits with the other soil items (item queries resolve to null and are skipped)
            int lastFertilizerIndex = shop.Items.FindLastIndex(item => IsFertilizer(item.ItemId));
            if (lastFertilizerIndex >= 0)
            {
                shop.Items.Insert(lastFertilizerIndex + 1, shopItem);
            }
            else
            {
                shop.Items.Add(shopItem);
            }
        }

        private void AddFieldNotesToShop(ShopData shop)
        {
            shop.Items ??= new List<ShopItemData>();

            if (shop.Items.Exists(item => item.Id == CropTraits.JOURNAL_ID))
            {
                return;
            }

            ShopItemData shopItem = new ShopItemData
            {
                Id = CropTraits.JOURNAL_ID,
                ItemId = CropTraits.JOURNAL_QUALIFIED_ID,
                Price = FIELD_NOTES_SHOP_PRICE,
                Condition = MailManager.FIELD_NOTES_SHOP_CONDITION
            };

            int fertilizerIndex = shop.Items.FindIndex(item => item.Id == CropTraits.MUTAGENIC_FERTILIZER_ID);
            if (fertilizerIndex >= 0)
            {
                shop.Items.Insert(fertilizerIndex + 1, shopItem);
            }
            else
            {
                shop.Items.Add(shopItem);
            }
        }

        private static bool IsFertilizer(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            return ItemRegistry.GetData(itemId)?.Category == FERTILIZER_CATEGORY;
        }
    }
}
