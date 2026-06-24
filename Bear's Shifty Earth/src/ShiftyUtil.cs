using System;
using System.Reflection;
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
        #region Structs

        /// <summary>
        /// A struct containing all configured properties related to Shifty Earth logic.
        /// </summary>
        public struct ShiftyProps
        {
            #region Fields

            public bool permStable;
            public short requiredSupport;
            public short adjacentSupport;
            public short belowSupport;
            public short topSupport;
            public short rainPenalty;
            public short plantBonus;

            #endregion Fields
        }

        #endregion Structs

        #region Fields

        [ThreadStatic]
        public static BlockPos scanPos = new(0);

        #endregion Fields

        #region Methods

        public static bool IsUnstable(IWorldAccessor world, BlockPos pos, ShiftyProps props)
        {
            // skip this if we obviously want it to be permanently stable
            if (props.requiredSupport <= -100) { return false; }

            if (scanPos == null) {
                scanPos = new BlockPos(pos.dimension);
            }

            int effectiveSupport = 0;

            IBlockAccessor blockAccessor = world.BlockAccessor;
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
                effectiveSupport += props.topSupport;
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
        /// Disables falling on the provided properties. Edits the actual property object, not a copy.
        /// </summary>
        public static ShiftyProps DisableFalling(ref ShiftyProps shiftyProps)
        {
            shiftyProps.permStable = true;
            return shiftyProps;
        }

        #endregion Methods
    }
}