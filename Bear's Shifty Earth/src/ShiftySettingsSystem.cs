using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BearsShiftyEarth
{
    public class ShiftySettings
    {
        #region Enums

        public enum FallingBehaviorFlag
        {
            Disabled,
            Vanilla,
            ShiftyEarth
        }

        #endregion Enums

        #region Fields

        public string BlocksAffectedComment { get => "Enable or disable behavior based on block type. Acceptable values are 0 (Disabled), 1 (Vanilla), and 2 (ShiftyEarth). Note that clay and farmland do not fall in their vanilla behaviors."; }
        public FallingBehaviorFlag SoilBehavior { get; set; } = FallingBehaviorFlag.ShiftyEarth;
        public FallingBehaviorFlag ClayBehavior { get; set; } = FallingBehaviorFlag.ShiftyEarth;
        public FallingBehaviorFlag PeatBehavior { get; set; } = FallingBehaviorFlag.ShiftyEarth;
        public FallingBehaviorFlag FarmlandBehavior { get; set; } = FallingBehaviorFlag.ShiftyEarth;
        public string SoilSupportRequiredComment { get => "The total number of support points soil needs to be stable, and what directions give that support. This is designed to give a lot of fine-tuned control over how you feel earth blocks should fall."; }
        public int SoilSupportRequired { get; set; } = 30;
        public int SoilAdjacentSupport { get; set; } = 10;
        public int SoilBelowSupport { get; set; } = 15;
        public int SoilAboveSupport { get; set; } = 5;
        public string ClaySupportRequiredComment { get => "Clay support. Clay has greater internal integrity and adheres to adjacent blocks more readily."; }
        public int ClaySupportRequired { get; set; } = 25;
        public int ClayAdjacentSupport { get; set; } = 15;
        public int ClayBelowSupport { get; set; } = 15;
        public int ClayAboveSupport { get; set; } = 5;
        public string PeatSupportRequiredComment { get => "Peat support. Peat has low internal integrity and is naturally wetter and sludgier."; }
        public int PeatSupportRequired { get; set; } = 40;
        public int PeatAdjacentSupport { get; set; } = 10;
        public int PeatBelowSupport { get; set; } = 10;
        public int PeatAboveSupport { get; set; } = 0;
        public string FarmSupportRequiredComment { get => "Farmland support. Farmland is light and aerated but looser than natural soil and crumbles easily. It benefits greatly from having crops on it."; }
        public int FarmSupportRequired { get; set; } = 20;
        public int FarmAdjacentSupport { get; set; } = 5;
        public int FarmBelowSupport { get; set; } = 15;
        public int FarmAboveSupport { get; set; } = 0;
        public string PlantsComment { get => "Root systems help to stabilize soil. GrassCoverBonus indicates the bonus a soil block gets from having full grass coverage. Patchy will have 2/3 of this value, while sparse will have 1/3. PlantHostBonus indicates the support bonus granted by a plant block on its top surface, such as tall grass or a cattail. These two bonuses stack. Large plant blocks are assumed to have deep roots that fill the interior of the soil block and provide significant structural integrity, while grass is not as deep."; }
        public int GrassCoverBonus { get; set; } = 5;
        public int PlantHostBonus { get; set; } = 10;
        public string EnvironmentComment { get => "MaximumStormPenalty is a penalty to support given to any block exposed to heavy rain. This penalty is scaled down for less intense rain. By default, clay is sturdier than soil when dry but becomes LESS sturdy when wet. Peat is naturally very sludgy and becomes nearly impossible to support when wet. Farmland similarly absorbs water readily and becomes mud."; }
        public int MaximumSoilStormPenalty { get; set; } = -25;
        public int MaximumClayStormPenalty { get; set; } = -30;
        public int MaximumPeatStormPenalty { get; set; } = -40;
        public int MaximumFarmStormPenalty { get; set; } = -30;
        public string FallChanceComment { get => "If not properly supported, what chance should each earth type have of falling when triggered. Expressed as a decimal between 0 and 1."; }
        public float SoilFallChance { get; set; } = 0.4f;
        public float ClayFallChance { get; set; } = 0.4f;
        public float PeatFallChance { get; set; } = 0.75f;
        public float FarmFallChance { get; set; } = 0.75f;

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
                }

                api.StoreModConfig<ShiftySettings>(SETTINGS, SettingsFilename);
            }
            catch (System.Exception ex) {
                Mod.Logger.Error($"{Lang.Get("bearsshiftyearth:settings-load-error")} | {ex.Message}");
                SETTINGS = new ShiftySettings();
            }
        }

        #endregion Methods
    }
}