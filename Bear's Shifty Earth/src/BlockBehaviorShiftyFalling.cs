using System;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BearsShiftyEarth
{
    /// <summary>
    /// Custom block behavior for Shifty Earth logic. Inherits from BlockBehaviorUnstableFalling and acts as a wrapper for the vanilla falling logic. Does not replace or overwrite UnstableFalling's logic and so should be safe to patch from other mods.
    /// </summary>
    public class BlockBehaviorShiftyFalling : BlockBehaviorUnstableFalling
    {
        #region Properties

        public ShiftyProps ShiftyProps { get => props; }

        #endregion Properties

        #region Fields

        [ThreadStatic]
        private static BlockPos? scanPos;

        private bool isShifty = false;
        private int requiredSupport;
        private int adjacentSupport;
        private int belowSupport;
        private int topSupport;
        private int rainPenalty;
        private int plantBonus;

        private ShiftyProps props;

        #endregion Fields

        #region Constructors

        public BlockBehaviorShiftyFalling(Block block) : base(block)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// As the block is finalized, configure and precache as much of the fiddly little options as possible.
        /// </summary>
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }

        #endregion Methods

        #region Methods

        /// <summary>
        /// OnBlockPlaced by default just triggers TryFalling, so we override it to inject our own code.
        /// </summary>
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Client) {
                return;
            }

            // skip all custom logic if we are not a shifty block, or fall if we are a shifty block but unstable.
            if (!isShifty || IsUnstable(world, blockPos)) {
                base.OnBlockPlaced(world, blockPos, ref handling);
            }
        }

        /// <summary>
        /// OnNeighborBlockChange by default just tries to trigger a fall, so we override it to inject our own support code.
        /// </summary>
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Client) {
                return;
            }

            // default to vanilla if we are not a shifty block or do vanilla if we ARE a shifty block
            if (!isShifty || IsUnstable(world, pos)) {
                base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
            }
        }

        /// <summary>
        /// Determines if a block is unstable or stable. If true, the block is unstable and at risk of falling. If true, the block is stable and will not attempt to fall.
        /// </summary>
        public bool IsUnstable(IWorldAccessor world, BlockPos pos)
        {
            // skip this if we obviously want it to be permanently stable
            if (requiredSupport <= -100) { return false; }

            if (scanPos == null) {
                scanPos = new BlockPos(pos.dimension);
            }

            int effectiveSupport = 0;

            IBlockAccessor blockAccessor = world.BlockAccessor;
            Block scannedBlock;

            // if we are exposed to the sky, penalize support based on the amount of rainfall. Light drizzle isn't enough to
            // trigger any penalty, I've noticed, so no need for special logic there
            if (blockAccessor.GetRainMapHeightAt(pos.X, pos.Z) <= pos.Y) {
                effectiveSupport += (int)world.BlockAccessor.GetClimateAt(pos).Rainfall * rainPenalty;
            }

            // check for support below
            _ = scanPos.Set(pos.X, pos.Y - 1, pos.Z);
            scannedBlock = blockAccessor.GetBlock(scanPos);
            if (scannedBlock.SideSolid[BlockFacing.UP.Index]) {
                effectiveSupport += belowSupport;
                if (effectiveSupport >= requiredSupport) {
                    return false;
                }
            }

            // check for support on the sides
            for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++) {
                BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
                _ = scanPos.Set(pos.X + blockFacing.Normali.X, pos.Y, pos.Z + blockFacing.Normali.Z);
                scannedBlock = blockAccessor.GetBlock(scanPos);

                if (scannedBlock.SideSolid[blockFacing.Opposite.Index] || scannedBlock.Code.Domain == "terrainslabs") {
                    effectiveSupport += adjacentSupport;
                    if (effectiveSupport >= requiredSupport) {
                        return false;
                    }
                }
            }

            // add solid top face
            _ = scanPos.Set(pos.X, pos.Y + 1, pos.Z);
            scannedBlock = blockAccessor.GetBlock(scanPos);
            if (scannedBlock.SideSolid[BlockFacing.DOWN.Index] || scannedBlock.Code.Domain == "terrainslabs") {
                effectiveSupport += topSupport;
                if (effectiveSupport >= requiredSupport) {
                    return false;
                }
            }

            // if we're still not supported, check for a plant on top
            scannedBlock = blockAccessor.GetBlock(scanPos);
            if (scannedBlock is BlockPlant or BlockCrop) {
                effectiveSupport += plantBonus;
                if (effectiveSupport >= requiredSupport) {
                    return false;
                }
            }

            return true;
        }

        #endregion Methods

        #region Helper Functions

        /// <summary>
        /// Checks to see if the block is configured to be a shifty block and should have our custom logic applied. Returns true if it should be targeted by custom logic, false if it behaves as vanilla.
        /// </summary>
        public (bool, EarthType) IsWhatShiftyBlock()
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
        /// Bakes the grass coverage bonus into the block's behavior as a reduction to the required support.
        /// </summary>
        public void AddGrassCoverageSupport(ShiftySettings config)
        {
            // check for grass coverage
            switch (block.LastCodePart()) {
                case BlockCodes.SPARSE_GRASS_CODE:
                    requiredSupport -= (int)(config.GrassCoverBonus * 0.33f);
                    break;

                case BlockCodes.PATCHY_GRASS_CODE:
                    requiredSupport -= (int)(config.GrassCoverBonus * 0.66f);
                    break;

                case BlockCodes.GRASSY_GRASS_CODE:
                    requiredSupport -= config.GrassCoverBonus;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Configures the behavior based on the injected mod settings.
        /// </summary>
        public void ConfigureBehavior(ShiftySettings config)
        {
            // make sure we're only being shifty with certain blocks
            (isShifty, EarthType shiftyType) = IsWhatShiftyBlock();

            if (!isShifty) {
                return;
            }

            // one of the only options not earth-typed. Grass is precached based on the block code later.
            plantBonus = config.PlantHostBonus;

            // set properties based on configured settings
            // penalties and bonuses are cached and precalculated for improved performance
            switch (shiftyType) {
                case EarthType.NONE:
                    isShifty = false;
                    break;

                case EarthType.Soil:
                    ConfigureAsSoil(config);
                    break;

                case EarthType.Clay:
                    ConfigureAsClay(config);
                    break;

                case EarthType.Peat:
                    ConfigureAsPeat(config);
                    break;

                case EarthType.Farmland:
                    ConfigureAsFarmland(config);
                    break;

                default:
                    isShifty = false;
                    break;
            }

            AddGrassCoverageSupport(config);
        }

        /// <summary>
        /// Configures the shifty block using soil-based settings.
        /// </summary>
        public void ConfigureAsSoil(ShiftySettings config)
        {
            if (config.SoilBehavior is ShiftySettings.FallingBehaviorFlag.Vanilla) {
                isShifty = false;
            }
            else if (config.SoilBehavior is ShiftySettings.FallingBehaviorFlag.Disabled) {
                DisableFalling(); // setting support to an absurdly low number prevents the vanilla logic from ever triggering
            }
            else {
                requiredSupport = config.SoilSupportRequired;
                adjacentSupport = config.SoilAdjacentSupport;
                belowSupport = config.SoilBelowSupport;
                topSupport = config.SoilAboveSupport;
                rainPenalty = config.MaximumSoilStormPenalty;
                fallSideways = true;

                // special code handling for forest floors, which otherwise count as soil
                if (block.FirstCodePart() == BlockCodes.FORESTFLOOR_CODE) {
                    if (config.ForestFloorIsStable) {
                        DisableFalling();
                    }
                    else {
                        requiredSupport -= config.ForestFloorBonus;
                    }
                }
                _ = SetFallChance(config.SoilFallChance);
            }
        }

        /// <summary>
        /// Configures the shifty block using peat settings.
        /// </summary>
        public void ConfigureAsPeat(ShiftySettings config)
        {
            if (config.PeatBehavior is ShiftySettings.FallingBehaviorFlag.Vanilla) {
                isShifty = false;
            }
            else if (config.PeatBehavior is ShiftySettings.FallingBehaviorFlag.Disabled) {
                DisableFalling();
            }
            else {
                requiredSupport = config.PeatSupportRequired;
                adjacentSupport = config.PeatAdjacentSupport;
                belowSupport = config.PeatBelowSupport;
                topSupport = config.PeatAboveSupport;
                rainPenalty = config.MaximumPeatStormPenalty;
                fallSideways = true;
                _ = SetFallChance(config.PeatFallChance);
            }
        }

        /// <summary>
        /// Configures the shifty block to use farmland settings.
        /// </summary>
        public void ConfigureAsFarmland(ShiftySettings config)
        {
            if (config.FarmlandBehavior is ShiftySettings.FallingBehaviorFlag.Vanilla or ShiftySettings.FallingBehaviorFlag.Disabled) {
                DisableFalling(); // in case it somehow falls through the earlier behavior assignment
            }
            requiredSupport = config.FarmSupportRequired;
            adjacentSupport = config.FarmAdjacentSupport;
            belowSupport = config.FarmBelowSupport;
            topSupport = config.FarmAboveSupport;
            rainPenalty = config.MaximumFarmStormPenalty;
            fallSideways = true;
            _ = SetFallChance(config.FarmFallChance);
        }

        /// <summary>
        /// Configures the shifty block to use clay settings.
        /// </summary>
        public void ConfigureAsClay(ShiftySettings config)
        {
            // clay does not fall in vanilla
            if (config.ClayBehavior is ShiftySettings.FallingBehaviorFlag.Vanilla or ShiftySettings.FallingBehaviorFlag.Disabled) {
                DisableFalling(); // in case it somehow falls through the earlier behavior assignment
            }
            else {
                requiredSupport = config.ClaySupportRequired;
                adjacentSupport = config.ClayAdjacentSupport;
                belowSupport = config.ClayBelowSupport;
                topSupport = config.ClayAboveSupport;
                rainPenalty = config.MaximumClayStormPenalty;
                fallSideways = true;
                _ = SetFallChance(config.ClayFallChance);
            }
        }

        /// <summary>
        /// Disables the falling logic entirely by making the blocks require negative support.
        /// </summary>
        private void DisableFalling()
        {
            requiredSupport = -100;
        }

        /// <summary>
        /// The actual chance to fall is stored in a private field, so we can't access it even as a child class. This uses the sinister, forbidden magic of reflection to access and set it anyway.
        /// Returns true if the fall chance was successfully set, false if there was a problem, just in case branching logic is desired later.
        /// </summary>
        private bool SetFallChance(float newChance)
        {
            FieldInfo? chanceField = typeof(BlockBehaviorUnstableFalling).GetField("fallSidewaysChance", BindingFlags.NonPublic | BindingFlags.Instance);

            if (chanceField == null) {
                return false;
            }

            chanceField.SetValue(this, newChance);

            //ModMain.Logger?.Chat($"Block ${block.Code} has a fall chance of {chanceField?.GetValue(this)?.ToString()}");

            return true;
        }

        #endregion Helper Functions
    }
}