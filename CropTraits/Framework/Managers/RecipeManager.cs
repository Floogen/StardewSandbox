using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;

namespace CropTraits.Framework.Managers
{
    public class RecipeManager : BaseManager
    {
        public const string SEED_MAKER_RECIPE_NAME = "Seed Maker";
        public const int SEED_MAKER_FARMING_LEVEL = 3;

        public const string SEED_SPLICER_INGREDIENTS = "709 10 336 2 787 1"; // Hardwood (10), Gold Bar (2) and a Battery Pack
        public const int SEED_SPLICER_FARMING_LEVEL = 9;

        public const int CRAFTING_RECIPE_UNLOCK_CONDITION_INDEX = 4;

        public RecipeManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public void EditCraftingRecipes(IDictionary<string, string> data)
        {
            SetFarmingUnlockLevel(data, SEED_MAKER_RECIPE_NAME, SEED_MAKER_FARMING_LEVEL);

            data[CropTraits.SEED_SPLICER_ID] = $"{SEED_SPLICER_INGREDIENTS}/Home/{CropTraits.SEED_SPLICER_ID}/true/{GetFarmingUnlockCondition(SEED_SPLICER_FARMING_LEVEL)}/";
        }

        public void LearnMissingRecipes()
        {
            LearnIfFarmingLevelReached(Game1.player, SEED_MAKER_RECIPE_NAME, SEED_MAKER_FARMING_LEVEL);
            LearnIfFarmingLevelReached(Game1.player, CropTraits.SEED_SPLICER_ID, SEED_SPLICER_FARMING_LEVEL);
        }

        private string GetFarmingUnlockCondition(int farmingLevel)
        {
            return $"s Farming {farmingLevel}";
        }

        private void SetFarmingUnlockLevel(IDictionary<string, string> data, string recipeName, int farmingLevel)
        {
            if (data.TryGetValue(recipeName, out string recipe) is false)
            {
                monitor.Log($"Couldn't find the {recipeName} crafting recipe, so its unlock level wasn't changed.", LogLevel.Warn);
                return;
            }

            string[] fields = recipe.Split('/');
            if (fields.Length <= CRAFTING_RECIPE_UNLOCK_CONDITION_INDEX)
            {
                monitor.Log($"The {recipeName} crafting recipe has an unexpected format, so its unlock level wasn't changed.", LogLevel.Warn);
                return;
            }

            fields[CRAFTING_RECIPE_UNLOCK_CONDITION_INDEX] = GetFarmingUnlockCondition(farmingLevel);
            data[recipeName] = string.Join('/', fields);
        }

        private void LearnIfFarmingLevelReached(Farmer farmer, string recipeName, int farmingLevel)
        {
            if (farmer is null || farmer.farmingLevel.Value < farmingLevel || farmer.craftingRecipes.ContainsKey(recipeName))
            {
                return;
            }

            farmer.craftingRecipes.Add(recipeName, 0);
        }
    }
}
