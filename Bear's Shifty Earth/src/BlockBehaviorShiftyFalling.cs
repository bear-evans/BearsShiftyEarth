using System;
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
        #region Fields

        [ThreadStatic]
        private static BlockPos? scanPos;

        private bool isShifty = false;
        private int requiredSupport;
        private int rainPenalty;
        private int plantBonus;

        #endregion Fields

        #region Constructors

        public BlockBehaviorShiftyFalling(Block block) : base(block)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            // make sure we're only being shifty with certain blocks
            (isShifty, BearsShiftyEarthModSystem.EarthType shiftyType) = IsWhatShiftyBlock(block);

            if (!isShifty) {
                return;
            }

            requiredSupport = ShiftySettingsSystem.Settings.SupportRequired;
            plantBonus = ShiftySettingsSystem.Settings.PlantHostBonus;

            // set properties based on configured settings
            // penalties and bonuses are cached and precalculated for improved performance
            switch (shiftyType) {
                case BearsShiftyEarthModSystem.EarthType.NONE:
                    break;

                case BearsShiftyEarthModSystem.EarthType.Soil:
                    rainPenalty = ShiftySettingsSystem.Settings.MaximumSoilStormPenalty;
                    break;

                case BearsShiftyEarthModSystem.EarthType.Clay:
                    requiredSupport -= ShiftySettingsSystem.Settings.ClayModifier;
                    rainPenalty = ShiftySettingsSystem.Settings.MaximumClayStormPenalty;
                    break;

                case BearsShiftyEarthModSystem.EarthType.Peat:
                    requiredSupport -= ShiftySettingsSystem.Settings.PeatModifier;
                    rainPenalty = ShiftySettingsSystem.Settings.MaximumPeatStormPenalty;
                    break;

                default:
                    break;
            }

            // check for grass coverage
            switch (block.LastCodePart()) {
                case "verysparse":
                    requiredSupport -= (int)(ShiftySettingsSystem.Settings.GrassCoverBonus * 0.33f);
                    BearsShiftyEarthModSystem.Logger.Notification($"Block with code ${block.Code} has a grass support bonus of {(int)(ShiftySettingsSystem.Settings.GrassCoverBonus * 0.33f)}");
                    break;

                case "sparse":
                    requiredSupport -= (int)(ShiftySettingsSystem.Settings.GrassCoverBonus * 0.66f);
                    BearsShiftyEarthModSystem.Logger.Notification($"Block with code ${block.Code} has a grass support bonus of {(int)(ShiftySettingsSystem.Settings.GrassCoverBonus * 0.66f)}");
                    break;

                case "normal":
                    requiredSupport -= ShiftySettingsSystem.Settings.GrassCoverBonus;
                    BearsShiftyEarthModSystem.Logger.Notification($"Block with code ${block.Code} has a grass support bonus of {ShiftySettingsSystem.Settings.GrassCoverBonus}");
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
            // skip all custom logic if we are not a shifty block.
            if (!isShifty) {
                base.OnBlockPlaced(world, blockPos, ref handling);
            }

            // TODO: obviously we want our own custom code in here eventually

            base.OnBlockPlaced(world, blockPos, ref handling);
        }

        /// <summary>
        /// OnNeighborBlockChange by default just tries to trigger a fall, so we override it to inject our own support code.
        /// </summary>
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            // default to vanilla if we are not a shifty block
            if (!isShifty) {
                base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
            }

            // TODO: Custom code redirects go here

            base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
        }

        /// <summary>
        /// Checks to see if the block is configured to be a shifty block and should have our custom logic applied. Returns true if it should be targeted by custom logic, false if it behaves as vanilla.
        /// </summary>
        public (bool, BearsShiftyEarthModSystem.EarthType) IsWhatShiftyBlock(Block block)
        {
            return block.FirstCodePart() switch {
                "soil" => (true, BearsShiftyEarthModSystem.EarthType.Soil),
                "peat" => (true, BearsShiftyEarthModSystem.EarthType.Clay),
                "rawclay" => (true, BearsShiftyEarthModSystem.EarthType.Peat),
                _ => (false, BearsShiftyEarthModSystem.EarthType.NONE),
            };
        }

        /// <summary>
        /// Determines if a block is unstable or stable. If true, the block is unstable and at risk of falling. If true, the block is stable and will not attempt to fall.
        /// </summary>
        public bool IsUnstable(IWorldAccessor world, BlockPos pos)
        {
            if (scanPos == null) {
                scanPos = new BlockPos(pos.dimension);
            }

            int support = 0;

            // Check if it's raining so we can get any penalties applied so we can still early-out
            //if (world.)

            IBlockAccessor blockAccessor = world.GetLockFreeBlockAccessor();

            // check for support below
            _ = scanPos.Set(pos.X, pos.Y - 1, pos.Z);
            if (blockAccessor.GetBlock(scanPos).SideSolid[BlockFacing.UP.Index]) {
                support += 15;
                if (support >= requiredSupport) {
                    return false;
                }
            }

            // check for support on the sides
            for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++) {
                BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
                _ = scanPos.Set(pos.X + blockFacing.Normali.X, pos.Y, pos.Z + blockFacing.Normali.Z);

                if (blockAccessor.GetBlock(scanPos).SideSolid[blockFacing.Opposite.Index]) {
                }
            }

            return true;
        }

        #endregion Methods
    }
}