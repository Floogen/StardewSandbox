using StardewModdingAPI.Utilities;

namespace CropTraits.Framework.Models
{
    public class ModConfig
    {
        // Shows badges over traited crops and lets hovering one show its traits
        public KeybindList ToggleTraitViewKeybind { get; set; } = KeybindList.Parse("G"); // TODO: Change this to be not set by default?

        // Opens the Crop Journal, unbound by default
        public KeybindList OpenJournalKeybind { get; set; } = new KeybindList();

        // Mature mutated crops twinkle until their first harvest
        public bool ShowMutationSparkles { get; set; } = true;

        // Lets traits like Slimy and Roosting be rolled and spawn monsters, turning both off when false
        public bool EnableMonsterTraits { get; set; } = true;
    }
}
