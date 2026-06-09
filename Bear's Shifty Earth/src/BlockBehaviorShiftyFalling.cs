using Vintagestory.API.Common;
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

        public bool isShifty = false;

        #endregion Fields

        #region Constructors

        public BlockBehaviorShiftyFalling(Block block) : base(block)
        {
        }

        #endregion Constructors

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
        public bool IsThisBlockKindaShifty(Block block)
        {
            return false;
        }

        #endregion Methods
    }
}