using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BearsShiftyEarth
{
    /// <summary>
    /// Extracted utility fuctions and logic to a globally accessible class. Makes compatibility easier as patched blocks from other mods can still use the same exact logic.
    /// </summary>
    public static class ShiftyUtil
    {
        #region Fields

        public static bool[]? Solids { get => SOLIDS; set => SOLIDS = value; }

        [ThreadStatic]
        public static BlockPos scanPos = new(0);

        private static bool[]? SOLIDS;

        #endregion Fields

        #region Methods

        /// <summary>The primary instability function. Returns true if the block is unstable and should attempt to fall.</summary>
        public static bool IsUnstable(IWorldAccessor world, BlockPos pos, ShiftyProps props)
        {
            // skip this if we obviously want it to be permanently stable
            if (props.permStable) { return false; }

            if (scanPos == null) {
                scanPos = new BlockPos(pos.dimension);
            }

            int effectiveSupport = 0;

            IBlockAccessor blockAccessor = world.GetLockFreeBlockAccessor();
            Block scannedBlock;

            // if we are exposed to the sky, penalize support based on the amount of rainfall. Light drizzle isn't enough to
            // trigger any penalty, I've noticed, so no need for special logic there
            if (blockAccessor.GetRainMapHeightAt(pos.X, pos.Z) <= pos.Y) {
                effectiveSupport += (int)world.BlockAccessor.GetClimateAt(pos).Rainfall * props.rainPenalty;
            }

            // check for support below
            _ = scanPos.Set(pos.X, pos.Y - 1, pos.Z);
            scannedBlock = blockAccessor.GetBlock(scanPos);
            if (scannedBlock.SideSolid[BlockFacing.UP.Index]) {
                effectiveSupport += props.belowSupport;
                if (effectiveSupport >= props.requiredSupport) {
                    return false;
                }
            }

            // check for support on the sides
            for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++) {
                BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
                _ = scanPos.Set(pos.X + blockFacing.Normali.X, pos.Y, pos.Z + blockFacing.Normali.Z);
                scannedBlock = blockAccessor.GetBlock(scanPos);

                if (scannedBlock.SideSolid[blockFacing.Opposite.Index] || scannedBlock.Code.Domain == "terrainslabs") {
                    effectiveSupport += props.adjacentSupport;
                    if (effectiveSupport >= props.requiredSupport) {
                        return false;
                    }
                }
            }

            // add solid top face
            _ = scanPos.Set(pos.X, pos.Y + 1, pos.Z);
            scannedBlock = blockAccessor.GetBlock(scanPos);
            if (scannedBlock.SideSolid[BlockFacing.DOWN.Index] || scannedBlock.Code.Domain == "terrainslabs") {
                effectiveSupport += props.aboveSupport;
                if (effectiveSupport >= props.requiredSupport) {
                    return false;
                }
            }

            // if we're still not supported, check for a plant on top
            scannedBlock = blockAccessor.GetBlock(scanPos);
            if (scannedBlock is BlockPlant or BlockCrop) {
                effectiveSupport += props.plantBonus;
                if (effectiveSupport >= props.requiredSupport) {
                    return false;
                }
            }

            return true;
        }

        #endregion Methods
    }
}