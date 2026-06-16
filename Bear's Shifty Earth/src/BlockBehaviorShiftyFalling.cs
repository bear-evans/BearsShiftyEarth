using System;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

using Config = BearsShiftyEarth.ShiftySettingsSystem;
using ModMain = BearsShiftyEarth.BearsShiftyEarthModSystem;

namespace BearsShiftyEarth
{
    /// <summary>
    /// Custom block behavior for Shifty Earth logic. Inherits from BlockBehaviorUnstableFalling and acts as a wrapper for the vanilla falling logic. Does not replace or overwrite UnstableFalling's logic and so should be safe to patch from other mods.
    /// </summary>
    public class BlockBehaviorShiftyFalling : BlockBehaviorUnstableFalling
    {
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

            // make sure we're only being shifty with certain blocks
            (isShifty, EarthType shiftyType) = IsWhatShiftyBlock(block);

            if (!isShifty) {
                return;
            }

            // one of the only options not earth-typed. Grass is precached based on the block code later.
            plantBonus = Config.Settings.PlantHostBonus;

            // set properties based on configured settings
            // penalties and bonuses are cached and precalculated for improved performance
            // HACK: Definitely clean up this nightmare switch. Disabled falling logic should remove the behavior instead, this is just a quick and dirty version
            switch (shiftyType) {
                case EarthType.NONE:
                    break;

                case EarthType.Soil:
                    if (Config.Settings.SoilBehavior is ShiftySettings.FallingBehaviorFlag.Vanilla) {
                        isShifty = false;
                    }
                    else if (Config.Settings.SoilBehavior is ShiftySettings.FallingBehaviorFlag.Disabled) {
                        requiredSupport = -100; // setting support to an absurdly low number prevents the vanilla logic from ever triggering
                    }
                    else {
                        requiredSupport = Config.Settings.SoilSupportRequired;
                        adjacentSupport = Config.Settings.SoilAdjacentSupport;
                        belowSupport = Config.Settings.SoilBelowSupport;
                        topSupport = Config.Settings.SoilAboveSupport;
                        rainPenalty = Config.Settings.MaximumSoilStormPenalty;
                        fallSideways = true;
                        _ = SetFallChance(Config.Settings.SoilFallChance);
                    }
                    break;

                case EarthType.Clay:
                    if (Config.Settings.ClayBehavior is ShiftySettings.FallingBehaviorFlag.Vanilla or ShiftySettings.FallingBehaviorFlag.Disabled) {
                        requiredSupport = -100; // in case it somehow falls through the earlier behavior assignment
                    }
                    else {
                        requiredSupport = Config.Settings.ClaySupportRequired;
                        adjacentSupport = Config.Settings.ClayAdjacentSupport;
                        belowSupport = Config.Settings.ClayBelowSupport;
                        topSupport = Config.Settings.ClayAboveSupport;
                        rainPenalty = Config.Settings.MaximumClayStormPenalty;
                        fallSideways = true;
                        _ = SetFallChance(Config.Settings.ClayFallChance);
                    }
                    break;

                case EarthType.Peat:
                    if (Config.Settings.PeatBehavior is ShiftySettings.FallingBehaviorFlag.Vanilla) {
                        isShifty = false;
                    }
                    else if (Config.Settings.PeatBehavior is ShiftySettings.FallingBehaviorFlag.Disabled) {
                        requiredSupport = -100;
                    }
                    else {
                        requiredSupport = Config.Settings.PeatSupportRequired;
                        adjacentSupport = Config.Settings.PeatAdjacentSupport;
                        belowSupport = Config.Settings.PeatBelowSupport;
                        topSupport = Config.Settings.PeatAboveSupport;
                        rainPenalty = Config.Settings.MaximumPeatStormPenalty;
                        fallSideways = true;
                        _ = SetFallChance(Config.Settings.PeatFallChance);
                    }
                    break;

                case EarthType.Farmland:
                    if (Config.Settings.FarmlandBehavior is ShiftySettings.FallingBehaviorFlag.Vanilla or ShiftySettings.FallingBehaviorFlag.Disabled) {
                        requiredSupport = -100; // in case it somehow falls through the earlier behavior assignment
                    }
                    requiredSupport = Config.Settings.FarmSupportRequired;
                    adjacentSupport = Config.Settings.FarmAdjacentSupport;
                    belowSupport = Config.Settings.FarmBelowSupport;
                    topSupport = Config.Settings.FarmAboveSupport;
                    rainPenalty = Config.Settings.MaximumFarmStormPenalty;
                    fallSideways = true;
                    _ = SetFallChance(Config.Settings.FarmFallChance);
                    break;

                default:
                    break;
            }
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

            IBlockAccessor blockAccessor = world.GetLockFreeBlockAccessor();

            // if we are exposed to the sky, penalize support based on the amount of rainfall. Light drizzle isn't enough to
            // trigger any penalty, I've noticed, so no need for special logic there
            if (blockAccessor.GetRainMapHeightAt(pos.X, pos.Z) <= pos.Y) {
                effectiveSupport += (int)world.BlockAccessor.GetClimateAt(pos).Rainfall * rainPenalty;
            }

            // check for support below
            _ = scanPos.Set(pos.X, pos.Y - 1, pos.Z);
            if (blockAccessor.GetBlock(scanPos).SideSolid[BlockFacing.UP.Index]) {
                effectiveSupport += belowSupport;
                if (effectiveSupport >= requiredSupport) {
                    return false;
                }
            }

            // check for support on the sides
            for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++) {
                BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
                _ = scanPos.Set(pos.X + blockFacing.Normali.X, pos.Y, pos.Z + blockFacing.Normali.Z);

                if (blockAccessor.GetBlock(scanPos).SideSolid[blockFacing.Opposite.Index]) {
                    effectiveSupport += adjacentSupport;
                    if (effectiveSupport >= requiredSupport) {
                        return false;
                    }
                }
            }

            // add solid top face
            _ = scanPos.Set(pos.X, pos.Y + 1, pos.Z);
            if (blockAccessor.GetBlock(scanPos).SideSolid[BlockFacing.DOWN.Index]) {
                effectiveSupport += topSupport;
                if (effectiveSupport >= requiredSupport) {
                    return false;
                }
            }

            // if we're still not supported, check for a plant on top
            if (blockAccessor.GetBlock(scanPos) is BlockPlant or BlockCrop) {
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
        public (bool, EarthType) IsWhatShiftyBlock(Block block)
        {
            return block.FirstCodePart() switch {
                BlockCodes.SOIL_CODE => (true, EarthType.Soil),
                BlockCodes.PEAT_CODE => (true, EarthType.Peat),
                BlockCodes.CLAY_CODE => (true, EarthType.Clay),
                BlockCodes.FARM_CODE => (true, EarthType.Farmland),
                _ => (false, EarthType.NONE),
            };
        }

        /// <summary>
        /// Bakes the grass coverage bonus into the block's behavior as a reduction to the required support.
        /// </summary>
        public void AddGrassCoverageSupport(Block block)
        {
            // check for grass coverage
            switch (block.LastCodePart()) {
                case BlockCodes.SPARSE_GRASS_CODE:
                    requiredSupport -= (int)(Config.Settings.GrassCoverBonus * 0.33f);
                    break;

                case BlockCodes.PATCHY_GRASS_CODE:
                    requiredSupport -= (int)(Config.Settings.GrassCoverBonus * 0.66f);
                    break;

                case BlockCodes.GRASSY_GRASS_CODE:
                    requiredSupport -= Config.Settings.GrassCoverBonus;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// The actual chance to fall is stored in a private field, so we can't access it even as a child class. This uses the sinister, forbidden magic of reflection to access and set it anyway.
        /// Returns true if the fall chance was successfully set, false if there was a problem, just in case branching logic is desired later.
        /// </summary>
        private bool SetFallChance(float newChance)
        {
            FieldInfo? chanceField = typeof(BlockBehaviorUnstableFalling).GetField("fallSidewaysChance", BindingFlags.NonPublic | BindingFlags.Instance);

            if (chanceField == null) {
                ModMain.Logger?.Error(Lang.Get("bearsshiftyearth:error-reflection-fallchance"));
                return false;
            }

            chanceField.SetValue(this, newChance);

            //ModMain.Logger?.Chat($"Block ${block.Code} has a fall chance of {chanceField?.GetValue(this)?.ToString()}");

            return true;
        }

        #endregion Helper Functions
    }
}