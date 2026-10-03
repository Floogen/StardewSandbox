using StardewModdingAPI.Utilities;

namespace CropTraits.Framework.Models
{
    public class ModConfig
    {
        // Shows badges over traited crops and lets hovering one show its traits
        public KeybindList ToggleTraitViewKeybind { get; set; } = KeybindList.Parse("G"); // TODO: Change this to be not set by default?

        // Mature mutated crops twinkle until their first harvest
        public bool ShowMutationSparkles { get; set; } = true;
    }
}
