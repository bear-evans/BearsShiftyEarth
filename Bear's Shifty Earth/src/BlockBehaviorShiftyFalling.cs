using Vintagestory.API.Common;
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
    }
}