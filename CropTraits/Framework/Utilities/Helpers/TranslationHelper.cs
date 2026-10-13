namespace CropTraits.Framework.Utilities.Helpers
{
    public static class TranslationHelper
    {
        public static string Get(string key, object tokens = null)
        {
            return CropTraits.modHelper.Translation.Get(key, tokens).ToString();
        }

        public static string GetOrDefault(string key, string fallback)
        {
            return CropTraits.modHelper.Translation.Get(key).Default(fallback).ToString();
        }
    }
}
