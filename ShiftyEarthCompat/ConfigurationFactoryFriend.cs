using System.Reflection;
using TerrainSlabs.Source.BlockBehaviors;
using Vintagestory.API.Common;
using FallFlag = BearsShiftyEarth.ShiftySettings.FallingBehaviorFlag;

namespace BearsShiftyEarth.Compat
{
    /// <summary>
    /// Configures ShiftyFalling blocks passed to it based on settings defined in the config.
    /// </summary>
    public class ConfigurationFactoryFriend
    {
        #region Fields

        private ShiftySettings config;

        #endregion Fields

        #region Constructors

        public ConfigurationFactoryFriend(ShiftySettings settings)
        {
            config = settings;
        }

        #endregion Constructors

        #region Configuration

        /// <summary>
        /// Configures the behavior based on the injected mod settings.
        /// </summary>
        public void ConfigureBehavior(BlockBehaviorShiftyFallingSlab behavior)
        {
            // make sure we're only being shifty with certain blocks
            (bool isShifty, EarthType shiftyType) = IsWhatShiftyBlock(behavior.block);

            behavior.IsShifty = isShifty;

            if (!isShifty) {
                return;
            }

            ShiftyProps props = new() {
                // one of the only options not earth-typed. Grass is precached based on the block code later.
                plantBonus = config.PlantHostBonus
            };

            // set properties based on configured settings
            // penalties and bonuses are cached and precalculated for improved performance
            switch (shiftyType) {
                case EarthType.NONE:
                    break;

                case EarthType.Soil:
                    ConfigureAsSoil(behavior);
                    break;

                case EarthType.Clay:
                    ConfigureAsClay(behavior);
                    break;

                case EarthType.Peat:
                    ConfigureAsPeat(behavior);
                    break;

                case EarthType.Farmland:
                    ConfigureAsFarmland(behavior);
                    break;

                default:
                    break;
            }

            AddGrassCoverageSupport(behavior);
        }

        #endregion Configuration

        #region Helper Functions

        /// <summary>
        /// Checks to see if the block is configured to be a shifty block and should have our custom logic applied. Returns true if it should be targeted by custom logic, false if it behaves as vanilla.
        /// </summary>
        public static (bool, EarthType) IsWhatShiftyBlock(Block block)
        {
            return block.FirstCodePart() switch {
                BlockCodes.SOIL_CODE => (true, EarthType.Soil),
                BlockCodes.PEAT_CODE => (true, EarthType.Peat),
                BlockCodes.CLAY_CODE => (true, EarthType.Clay),
                BlockCodes.FARM_CODE => (true, EarthType.Farmland),
                BlockCodes.FORESTFLOOR_CODE => (true, EarthType.Soil),
                _ => (false, EarthType.NONE),
            };
        }

        /// <summary>
        /// Sets the fall chance value on the provided Block Behavior. The behavior must be or inherit from BlockBehaviorUnstableFallingSlab.
        /// </summary>
        public static bool SetFallChance(BlockBehaviorUnstableFallingSlab behaveObj, float newChance)
        {
            FieldInfo? chanceField = typeof(BlockBehaviorUnstableFallingSlab).GetField("fallSidewaysChance", BindingFlags.NonPublic | BindingFlags.Instance);

            if (chanceField == null) {
                return false;
            }

            chanceField.SetValue(behaveObj, newChance);

            return true;
        }

        /// <summary>
        /// Sets the fall sideways on the provided Block Behavior. The behavior must be or inherit from BlockBehaviorUnstableFallingSlab.
        /// </summary>
        public static bool SetFallSidewaysChance(BlockBehaviorUnstableFallingSlab behaveObj, bool newChance)
        {
            FieldInfo? chanceField = typeof(BlockBehaviorUnstableFallingSlab).GetField("fallSideways", BindingFlags.NonPublic | BindingFlags.Instance);

            if (chanceField == null) {
                return false;
            }

            chanceField.SetValue(behaveObj, newChance);

            return true;
        }

        /// <summary>
        /// Disables falling on the provided properties. Edits the actual property object, not a copy.
        /// </summary>
        public static void DisableFalling(ref ShiftyProps shiftyProps)
        {
            shiftyProps.permStable = true;
        }

        /// <summary>
        /// Configures the shifty block using soil-based settings.
        /// </summary>
        public void ConfigureAsSoil(BlockBehaviorShiftyFallingSlab behavior)
        {
            if (config.SoilBehavior is FallFlag.Vanilla) {
                behavior.IsShifty = false;
            }
            else if (config.SoilBehavior is FallFlag.Disabled) {
                DisableFalling(ref behavior.Props); // setting support to an absurdly low number prevents the vanilla logic from ever triggering
            }
            else {
                behavior.RequiredSupport = config.SoilSupportRequired;
                behavior.AdjacentSupport = config.SoilAdjacentSupport;
                behavior.BelowSupport = config.SoilBelowSupport;
                behavior.AboveSupport = config.SoilAboveSupport;
                behavior.RainPenalty = config.MaximumSoilStormPenalty;
                behavior.PlantBonus = config.PlantHostBonus;
                _ = SetFallSidewaysChance(behavior, true);
                _ = SetFallChance(behavior, config.SoilFallChance);

                // special code handling for forest floors, which otherwise count as soil
                if (behavior.block.FirstCodePart() == BlockCodes.FORESTFLOOR_CODE) {
                    if (config.ForestFloorIsStable) {
                        DisableFalling(ref behavior.Props);
                    }
                    else {
                        behavior.RequiredSupport -= config.ForestFloorBonus;
                    }
                }
            }
        }

        /// <summary>
        /// Configures the shifty block using clay-based settings.
        /// </summary>
        public void ConfigureAsClay(BlockBehaviorShiftyFallingSlab behavior)
        {
            if (config.ClayBehavior is FallFlag.Vanilla or FallFlag.Disabled) {
                // clay does not fall in vanilla
                behavior.IsShifty = true;
                DisableFalling(ref behavior.Props);
            }
            else {
                behavior.RequiredSupport = config.ClaySupportRequired;
                behavior.AdjacentSupport = config.ClayAdjacentSupport;
                behavior.BelowSupport = config.ClayBelowSupport;
                behavior.AboveSupport = config.ClayAboveSupport;
                behavior.RainPenalty = config.MaximumClayStormPenalty;
                behavior.PlantBonus = config.PlantHostBonus;
                _ = SetFallSidewaysChance(behavior, true);
                _ = SetFallChance(behavior, config.ClayFallChance);
            }
        }

        /// <summary>
        /// Configures the shifty block using clay-based settings.
        /// </summary>
        public void ConfigureAsPeat(BlockBehaviorShiftyFallingSlab behavior)
        {
            if (config.PeatBehavior is FallFlag.Vanilla) {
                // peat falls in vanilla
                behavior.IsShifty = false;
            }
            else if (config.SoilBehavior is FallFlag.Disabled) {
                DisableFalling(ref behavior.Props);
            }
            else {
                behavior.RequiredSupport = config.PeatSupportRequired;
                behavior.AdjacentSupport = config.PeatAdjacentSupport;
                behavior.BelowSupport = config.PeatBelowSupport;
                behavior.AboveSupport = config.PeatAboveSupport;
                behavior.RainPenalty = config.MaximumPeatStormPenalty;
                behavior.PlantBonus = config.PlantHostBonus;
                _ = SetFallSidewaysChance(behavior, true);
                _ = SetFallChance(behavior, config.PeatFallChance);
            }
        }

        /// <summary>
        /// Configures the shifty block using clay-based settings.
        /// </summary>
        public void ConfigureAsFarmland(BlockBehaviorShiftyFallingSlab behavior)
        {
            if (config.FarmlandBehavior is FallFlag.Vanilla or FallFlag.Disabled) {
                // farmland does not fall in vanilla
                DisableFalling(ref behavior.Props);
            }
            else {
                behavior.RequiredSupport = config.FarmSupportRequired;
                behavior.AdjacentSupport = config.FarmAdjacentSupport;
                behavior.BelowSupport = config.FarmBelowSupport;
                behavior.AboveSupport = config.FarmAboveSupport;
                behavior.RainPenalty = config.MaximumFarmStormPenalty;
                behavior.PlantBonus = config.PlantHostBonus;
                _ = SetFallSidewaysChance(behavior, true);
                _ = SetFallChance(behavior, config.FarmFallChance);
            }
        }

        /// <summary>
        /// Bakes the grass coverage bonus into the block's behavior as a reduction to the required support.
        /// </summary>
        public void AddGrassCoverageSupport(BlockBehaviorShiftyFallingSlab behavior)
        {
            // check for grass coverage
            switch (behavior.block.LastCodePart()) {
                case BlockCodes.SPARSE_GRASS_CODE:
                    behavior.RequiredSupport -= (int)(config.GrassCoverBonus * 0.33f);
                    break;

                case BlockCodes.PATCHY_GRASS_CODE:
                    behavior.RequiredSupport -= (int)(config.GrassCoverBonus * 0.66f);
                    break;

                case BlockCodes.GRASSY_GRASS_CODE:
                    behavior.RequiredSupport -= config.GrassCoverBonus;
                    break;

                default:
                    break;
            }
        }

        #endregion Helper Functions
    }
}