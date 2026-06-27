using TerrainSlabs.Source.BlockBehaviors;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace BearsShiftyEarth.Compat
{
    /// <summary>
    /// Hacked together behavior for a Shifty Earth version of Terrain Slab's falling blocks.
    /// </summary>
    public class BlockBehaviorShiftyFallingSlab : BlockBehaviorUnstableFallingSlab
    {
        #region Properties

        /// <summary>The support and falling properties of this block. Returns a reference to the object, not a copy.</summary>
        public ref ShiftyProps Props { get => ref props; }

        /// <summary>The amount of support required to be ineligible to fall.</summary>
        public int RequiredSupport { get => props.requiredSupport; set => props.requiredSupport = value; }

        /// <summary>The support bonus the block gets per adjacent solid block.</summary>
        public int AdjacentSupport { get => props.adjacentSupport; set => props.adjacentSupport = value; }

        /// <summary>The support bonus the block gets for a solid block beneath it.</summary>
        public int BelowSupport { get => props.belowSupport; set => props.belowSupport = value; }

        /// <summary>The support bonus the block gets for having a solid block above it.</summary>
        public int AboveSupport { get => props.aboveSupport; set => props.aboveSupport = value; }

        /// <summary>The support bonus the block gets for having a plant or crop block above it.</summary>
        public int PlantBonus { get => props.plantBonus; set => props.plantBonus = value; }

        /// <summary>The maxmimum penalty the block gets when exposed to heavy rainfall.</summary>
        public int RainPenalty { get => props.rainPenalty; set => props.rainPenalty = value; }

        /// <summary>Whether or not this block should follow Shifty Earth support logic.</summary>
        public bool IsShifty { get => isShifty; set => isShifty = value; }

        #endregion Properties

        #region Fields

        private bool isShifty = false;

        private ShiftyProps props;

        #endregion Fields

        #region Constructors

        public BlockBehaviorShiftyFallingSlab(Block slab) : base(slab)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// OnBlockPlaced by default just triggers TryFalling, so we override it to inject our own code.
        /// </summary>
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
        {
#if DEBUG
            world.Api.Logger.Warning("Successfully hijacked terrain slabs logic!");
#endif
            if (world.Side == EnumAppSide.Client) {
                return;
            }

            // skip all custom logic if we are not a shifty block, or fall if we are a shifty block but unstable.
            if (!isShifty || ShiftyUtil.IsUnstable(world, blockPos, props)) {
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
            if (!isShifty || ShiftyUtil.IsUnstable(world, pos, props)) {
                base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
            }
        }

        #endregion Methods
    }
}