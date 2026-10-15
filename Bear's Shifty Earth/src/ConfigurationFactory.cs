using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using FallFlag = BearsShiftyEarth.ShiftySettings.FallingBehaviorFlag;

namespace BearsShiftyEarth
{
    /// <summary>
    /// Configures ShiftyFalling blocks passed to it based on settings defined in the config.
    /// </summary>
    public class ConfigurationFactory
    {
        #region Fields

        private ShiftySettings config;
        private AssetLocation rockslideSound = new("game:sounds/effect/rockslide");

        #endregion Fields

        #region Constructors

        public ConfigurationFactory(ShiftySettings settings)
        {
            config = settings;
        }

        #endregion Constructors

        #region Configuration

        /// <summary>
        /// Configures the behavior based on the injected mod settings.
        /// </summary>
        public void ConfigureBehavior(BlockBehaviorShiftyFalling behavior)
        {
            // make sure we're only being shifty with certain blocks
            (bool isShifty, EarthType shiftyType) = IsWhatShiftyBlock(behavior.block);

            behavior.IsShifty = isShifty;

            if (!isShifty) {
                return;
            }

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
        /// Disables falling on the provided properties. Edits the actual property object, not a copy.
        /// </summary>
        public static void DisableFalling(ref ShiftyProps shiftyProps)
        {
            shiftyProps.permStable = true;
        }

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
        /// Sets the dustIntensity value on the provided Block Behavior. The behavior must be or inherit from BlockBehaviorUnstableFalling.
        /// </summary>
        public static bool SetDustIntensity(BlockBehaviorUnstableFalling behaveObj, float dusty)
        {
            FieldInfo? chanceField = typeof(BlockBehaviorUnstableFalling).GetField("dustIntensity", BindingFlags.NonPublic | BindingFlags.Instance);

            if (chanceField == null) {
                return false;
            }

            chanceField.SetValue(behaveObj, dusty);

            return true;
        }

        /// <summary>
        /// Sets the fall chance value on the provided Block Behavior. The behavior must be or inherit from BlockBehaviorUnstableFalling.
        /// </summary>
        public static bool SetFallChance(BlockBehaviorUnstableFalling behaveObj, float newChance)
        {
            FieldInfo? chanceField = typeof(BlockBehaviorUnstableFalling).GetField("fallSidewaysChance", BindingFlags.NonPublic | BindingFlags.Instance);

            if (chanceField == null) {
                return false;
            }

            chanceField.SetValue(behaveObj, newChance);

            return true;
        }

        /// <summary>
        /// Sets the falling sound. Does not verify asset's existence, do this before running this function.
        /// </summary>
        public static bool SetFallSound(BlockBehaviorUnstableFalling behaveObj, AssetLocation sound)
        {
            FieldInfo? chanceField = typeof(BlockBehaviorUnstableFalling).GetField("fallSound", BindingFlags.NonPublic | BindingFlags.Instance);

            if (chanceField == null) {
                return false;
            }

            chanceField.SetValue(behaveObj, sound);

            return true;
        }

        /// <summary>
        /// Bakes the grass coverage bonus into the block's behavior as a reduction to the required support.
        /// </summary>
        public void AddGrassCoverageSupport(BlockBehaviorShiftyFalling behavior)
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

        /// <summary>
        /// Configures the shifty block using clay-based settings.
        /// </summary>
        public void ConfigureAsClay(BlockBehaviorShiftyFalling behavior)
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
                behavior.fallSideways = true;

                _ = SetFallChance(behavior, config.ClayFallChance);
                _ = SetFallSound(behavior, rockslideSound);
                _ = SetDustIntensity(behavior, 0.1f);
            }
        }

        /// <summary>
        /// Configures the shifty block using clay-based settings.
        /// </summary>
        public void ConfigureAsFarmland(BlockBehaviorShiftyFalling behavior)
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
                behavior.fallSideways = true;

                _ = SetFallChance(behavior, config.FarmFallChance);
                _ = SetFallSound(behavior, rockslideSound);
                _ = SetDustIntensity(behavior, 0.3f);
            }
        }

        /// <summary>
        /// Configures the shifty block using clay-based settings.
        /// </summary>
        public void ConfigureAsPeat(BlockBehaviorShiftyFalling behavior)
        {
            if (config.PeatBehavior is FallFlag.Vanilla) {
                // peat falls in vanilla
                behavior.IsShifty = false;
            }
            else if (config.PeatBehavior is FallFlag.Disabled) {
                DisableFalling(ref behavior.Props);
            }
            else {
                behavior.RequiredSupport = config.PeatSupportRequired;
                behavior.AdjacentSupport = config.PeatAdjacentSupport;
                behavior.BelowSupport = config.PeatBelowSupport;
                behavior.AboveSupport = config.PeatAboveSupport;
                behavior.RainPenalty = config.MaximumPeatStormPenalty;
                behavior.PlantBonus = config.PlantHostBonus;
                behavior.fallSideways = true;

                _ = SetFallChance(behavior, config.PeatFallChance);
                _ = SetFallSound(behavior, rockslideSound);
                _ = SetDustIntensity(behavior, 0.2f);
            }
        }

        /// <summary>
        /// Configures the shifty block using soil-based settings.
        /// </summary>
        public void ConfigureAsSoil(BlockBehaviorShiftyFalling behavior)
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
                behavior.fallSideways = true;
                _ = SetDustIntensity(behavior, 0.3f);

                // special code handling for forest floors, which otherwise count as soil
                if (behavior.block.FirstCodePart() == BlockCodes.FORESTFLOOR_CODE) {
                    if (config.ForestFloorIsStable) {
                        DisableFalling(ref behavior.Props);
                    }
                    else {
                        behavior.RequiredSupport -= config.ForestFloorBonus;
                    }
                }
                _ = SetFallChance(behavior, config.SoilFallChance);
            }
        }

        #endregion Helper Functions
    }
}
