using System.Runtime.CompilerServices;
using Vintagestory.API.Common;

namespace BearsShiftyEarth.Compat
{
    /// <summary>
    /// Object that handles compatibility between Shifty Earth and terrain slabs.
    /// </summary>
    public class TerrainSlabsCompat : IModCompatHandler
    {
        #region Properties

        public Mod? FriendMod { get => modRef; set => modRef = value; }

        #endregion Properties

        #region Fields

        private Mod? modRef;

        #endregion Fields

        #region Constructors

        public TerrainSlabsCompat()
        {
        }

        #endregion Constructors

        #region Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AttachBehaviorsToBlocks(ShiftySettings config, ICoreAPI api)
        {
            throw new System.NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ConfigureCompatBlocks(ShiftySettings config, ConfigurationFactory factory)
        {
            throw new System.NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RegisterCompatBehaviors(ICoreAPI api)
        {
            api.RegisterBlockBehaviorClass("UnstableFallingSlab", typeof(BlockBehaviorShiftyFallingSlab));
        }

        #endregion Methods
    }
}