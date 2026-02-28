using StardewModdingAPI;
using StardewModdingAPI.Integrations.GenericModConfigMenu;
using StardewSandbox.Framework.Objects;
using StardewSandbox.Framework.Utilities;
using StardewValley;
using StardewValley.GameData.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSandbox.Framework.Managers
{
    public class ApiManager : BaseManager
    {
        private IGenericModConfigMenuApi _genericModConfigMenuApi;

        public ApiManager(IModHelper helper) : base(helper)
        {

        }

        internal void RegisterGenericModConfigMenu(IModHelper helper, IManifest modManifest)
        {            
            _genericModConfigMenuApi = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            _genericModConfigMenuApi.Register(modManifest, () => ResetConfig(), () => helper.WriteConfig(ModEntry.modConfig));

            _genericModConfigMenuApi.AddSectionTitle(modManifest, () => helper.Translation.Get("config.general.title"));

            _genericModConfigMenuApi.AddBoolOption(modManifest, () => ModEntry.modConfig.ShowGlowEffect, value => ModEntry.modConfig.ShowGlowEffect = value, () => helper.Translation.Get("config.general.show_glow_effect.name"), () => helper.Translation.Get("config.general.show_glow_effect.description"));
            _genericModConfigMenuApi.AddBoolOption(modManifest, () => ModEntry.modConfig.ShowClockEffect, value => ModEntry.modConfig.ShowClockEffect = value, () => helper.Translation.Get("config.general.show_clock_effect.name"), () => helper.Translation.Get("config.general.show_clock_effect.description"));
            _genericModConfigMenuApi.AddBoolOption(modManifest, () => ModEntry.modConfig.AutoConsumeCharges, value => ModEntry.modConfig.AutoConsumeCharges = value, () => helper.Translation.Get("config.general.auto_consume_charges.name"), () => helper.Translation.Get("config.general.auto_consume_charges.description"));
        }

        private static void ResetConfig()
        {
            ModEntry.modConfig = new ModConfig();
        }
    }
}
