using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BearsShiftyEarth
{
    public class ShiftySettings
    {
        #region Fields

        public string SupportRequiredComment { get => "The total number of support points needed to be stable. If the block has this many points or more, it will not fall. If it has less, it has a chance to fall. Higher values make blocks more difficult to support. Adjacent blocks give 10, a block below gives 15."; }
        public int SupportRequired { get; set; } = 30;
        public string BlockTraitModifiersComment { get => "What bonuses or penalties a block should have based on its composition. By default, clay is more solid than normal soil (unless wet), while peat is amorphous and prone to 'peat slides' and 'bog bursts.' This modifier is subtracted from the base support requirement."; }
        public int ClayModifier { get; set; } = 10;
        public int PeatModifier { get; set; } = -8;
        public string PlantsComment { get => "Root systems help to stabilize soil. GrassCoverBonus indicates the bonus a soil block gets from having full grass coverage. Patchy will have 2/3 of this value, while sparse will have 1/3. PlantHostBonus indicates the support bonus granted by a plant block on its top surface, such as tall grass or a fern. These two bonuses stack. Large plant blocks are assumed to have deep roots that fill the interior of the soil block and provide significant structural integrity, while grass is not as deep."; }
        public int GrassCoverBonus { get; set; } = 12;
        public int PlantHostBonus { get; set; } = 15;
        public string EnvironmentComment { get => "MaximumStormPenalty is a penalty to support given to any block exposed to heavy rain. This penalty is scaled down for less intense rain. By default, clay is sturdier than soil when dry but becomes LESS sturdy when wet. Peat is naturally very sludgy and becomes nearly impossible to support when wet."; }
        public int MaximumSoilStormPenalty { get; set; } = -8;
        public int MaximumClayStormPenalty { get; set; } = -20;
        public int MaximumPeatStormPenalty { get; set; } = -20;

        #endregion Fields
    }

    /// <summary>
    /// Mod System that handles loading of Bear's Shifty Earth settings
    /// </summary>
    public class ShiftySettingsSystem : ModSystem
    {
        #region Properties

        /// <summary> Reference to the currently loaded settings object. </summary>
        public static ShiftySettings Settings {
            get => SETTINGS ?? new ShiftySettings();
        }

        private string SettingsFilename { get => "BearsShiftyEarth.json"; }

        #endregion Properties

        #region Fields

        private static ShiftySettings? SETTINGS;

        #endregion Fields

        #region Methods

        /// <summary>
        /// Loads the mod config before all other mod loading processes.
        /// </summary>
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            // config doesn't matter to the client
            if (api.Side == EnumAppSide.Server) {
                LoadOrCreateSettings(api);
            }
        }

        /// <summary>
        /// Attempts to load the mod config from the settings file. If not found or an error occurs, creates a new one with default settings.
        /// </summary>
        private void LoadOrCreateSettings(ICoreAPI api)
        {
            try {
                SETTINGS = api.LoadModConfig<ShiftySettings>(SettingsFilename);

                if (SETTINGS == null) {
                    Mod.Logger.Notification(Lang.Get("bearsshiftyearth:settings-initializing-file"));
                    SETTINGS = new ShiftySettings();
                    api.StoreModConfig<ShiftySettings>(SETTINGS, SettingsFilename);
                }
            }
            catch (System.Exception ex) {
                Mod.Logger.Error($"{Lang.Get("bearsshiftyearth:settings-load-error")} | {ex.Message}");
                SETTINGS = new ShiftySettings();
            }
        }

        #endregion Methods
    }
}