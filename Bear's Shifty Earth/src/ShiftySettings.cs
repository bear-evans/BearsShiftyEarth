using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BearsShiftyEarth
{
    public class ShiftySettings
    {
    }

    /// <summary>
    /// Mod System that handles loading of Bear's Shifty Earth settings
    /// </summary>
    public class ShiftySettingsLoader : ModSystem
    {
        #region Properties

        /// <summary> Reference to the currently loaded settings object. </summary>
        public static ShiftySettings Settings {
            get => SETTINGS ?? new ShiftySettings();
        }

        private static string SettingsFilename { get => "bearsshiftyearth.json"; }

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
            LoadOrCreateSettings(api);
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