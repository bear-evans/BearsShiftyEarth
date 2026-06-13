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
            (isShifty, EarthType shiftyType) = IsWhatShiftyBlock(block);

            if (!isShifty) {
                return;
            }

            requiredSupport = Config.Settings.SupportRequired;
            plantBonus = Config.Settings.PlantHostBonus;

            // set properties based on configured settings
            // penalties and bonuses are cached and precalculated for improved performance
            switch (shiftyType) {
                case EarthType.NONE:
                    break;

                case EarthType.Soil:
                    rainPenalty = Config.Settings.MaximumSoilStormPenalty;
                    _ = SetFallChance(Config.Settings.SoilFallChance);
                    break;

                case EarthType.Clay:
                    requiredSupport -= Config.Settings.ClayModifier;
                    rainPenalty = Config.Settings.MaximumClayStormPenalty;
                    _ = SetFallChance(Config.Settings.ClayFallChance);
                    break;

                case EarthType.Peat:
                    requiredSupport -= Config.Settings.PeatModifier;
                    rainPenalty = Config.Settings.MaximumPeatStormPenalty;
                    _ = SetFallChance(Config.Settings.PeatFallChance);
                    break;

                default:
                    break;
            }

            // check for grass coverage
            switch (block.LastCodePart()) {
                case "verysparse":
                    requiredSupport -= (int)(Config.Settings.GrassCoverBonus * 0.33f);
                    break;

                case "sparse":
                    requiredSupport -= (int)(Config.Settings.GrassCoverBonus * 0.66f);
                    break;

                case "normal":
                    requiredSupport -= Config.Settings.GrassCoverBonus;
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
            // default to vanilla if we are not a shifty block or do vanilla if we ARE a shifty block
            if (!isShifty || IsUnstable(world, pos)) {
                base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
            }
        }

        /// <summary>
        /// Checks to see if the block is configured to be a shifty block and should have our custom logic applied. Returns true if it should be targeted by custom logic, false if it behaves as vanilla.
        /// </summary>
        public (bool, EarthType) IsWhatShiftyBlock(Block block)
        {
            return block.FirstCodePart() switch {
                "soil" => (true, EarthType.Soil),
                "peat" => (true, EarthType.Peat),
                "rawclay" => (true, EarthType.Clay),
                _ => (false, EarthType.NONE),
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

            int effectiveSupport = 0;

            IBlockAccessor blockAccessor = world.GetLockFreeBlockAccessor();

            // if we are exposed to the sky, penalize support based on the amount of rainfall. Light drizzle isn't enough to
            // trigger any penalty, I've noticed, so no need for special logic there
            if (blockAccessor.GetRainMapHeightAt(pos.X, pos.Z) <= pos.Y) {
                effectiveSupport += (int)world.BlockAccessor.GetClimateAt(pos).Rainfall * rainPenalty;
#if DEBUG
                ModMain.Logger?.Chat($"Block ${block.Code} has rain penalty of {effectiveSupport}");
#endif
            }

            // check for support below
            _ = scanPos.Set(pos.X, pos.Y - 1, pos.Z);
            if (blockAccessor.GetBlock(scanPos).SideSolid[BlockFacing.UP.Index]) {
                effectiveSupport += 15;
#if DEBUG
                ModMain.Logger?.Chat($"Block ${block.Code} has block beneath, current support is {effectiveSupport}");
#endif
                if (effectiveSupport >= requiredSupport) {
                    return false;
                }
            }

            // check for support on the sides
            for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++) {
                BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
                _ = scanPos.Set(pos.X + blockFacing.Normali.X, pos.Y, pos.Z + blockFacing.Normali.Z);

                if (blockAccessor.GetBlock(scanPos).SideSolid[blockFacing.Opposite.Index]) {
                    effectiveSupport += 10;
#if DEBUG
                    ModMain.Logger?.Chat($"Block ${block.Code} has solid block at face {BlockFacing.HORIZONTALS[i]}, current support is {effectiveSupport}");
#endif
                    if (effectiveSupport >= requiredSupport) {
                        return false;
                    }
                }
            }

            // if we're still not supported, check for a plant on top
            _ = scanPos.Set(pos.X, pos.Y + 1, pos.Z);
            if (blockAccessor.GetBlock(scanPos) is BlockPlant or BlockCrop) {
                effectiveSupport += plantBonus;
#if DEBUG
                ModMain.Logger?.Chat($"Block ${block.Code} has plant on top, current support is {effectiveSupport}");

#endif
                if (effectiveSupport >= requiredSupport) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The actual chance to fall is stored in a private field, so we can't access it even as a child class. This uses the dark magic of reflection to access and set it anyway.
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

            ModMain.Logger?.Chat($"Block ${block.Code} has a fall chance of {chanceField?.GetValue(this)?.ToString()}");

            return true;
        }

        #endregion Methods
    }
}