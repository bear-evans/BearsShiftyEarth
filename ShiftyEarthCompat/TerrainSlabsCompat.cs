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
        private ConfigurationFactoryFriend? factory;

        #endregion Fields

        #region Constructors

        public TerrainSlabsCompat()
        {
        }

        #endregion Constructors

        #region Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ConfigureCompatBlock(Block block, ShiftySettings config)
        {
            factory ??= new ConfigurationFactoryFriend(config);

            // if it has this behavior, it's a terrain slab block that needs configured
            if (block.GetBehavior<BlockBehaviorShiftyFallingSlab>() is BlockBehaviorShiftyFallingSlab behavior) {
                factory.ConfigureBehavior(behavior);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RegisterCompatBehaviors(ICoreAPI api)
        {
            api.RegisterBlockBehaviorClass("UnstableFallingSlab", typeof(BlockBehaviorShiftyFallingSlab));
        }

        #endregion Methods
    }
}