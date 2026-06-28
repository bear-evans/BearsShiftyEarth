using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

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

            // first, add falling code if it's clay and configured for this
            if (block.Code.FirstCodePart() is BlockCodes.CLAY_CODE && config.ClayBehavior == ShiftySettings.FallingBehaviorFlag.ShiftyEarth) {
                // don't duplicate
                if (block.HasBehavior<BlockBehaviorShiftyFallingSlab>()) {
                    return;
                }
                else {
                    AttachShiftyBehavior(block, factory);
                }
            }

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

        public void AttachShiftyBehavior(Block block, ConfigurationFactoryFriend factory)
        {
            // these don't actually mean anything right now, it just wanted a JSON and I didn't feel
            // like rewriting all the in-behavior
            // configuration logic to appease the machine. It's here in case I want to for mod compatibility.
            JsonObject shiftyProperties = JsonObject.FromJson(@"{
                        ""requiredSupport"": 8,
                        ""adjacentSupport"": 5,
                        ""belowSupport"": 15,
                        ""topSupport"": 5,
                        ""rainPenalty"": -10,
                        ""plantBonus"": 15,
                        ""fallChance"": 0.5
                    }");

            // manually add the block behavior if configured to do so
            BlockBehaviorShiftyFallingSlab shiftyFallingBehavior = new(block);
            shiftyFallingBehavior.Initialize(shiftyProperties);

            // for some reason we have to configure the behavior here because it gets missed by the ConfigureBehavior call in the main thread.
            // apparently GetBehavior needs some kind of preregistration and it isn't catching the manually added ones.
            factory.ConfigureBehavior(shiftyFallingBehavior);

            List<BlockBehavior> behaviorsList = block.BlockBehaviors.ToList();
            behaviorsList.Add(shiftyFallingBehavior);
            block.BlockBehaviors = behaviorsList.ToArray();
        }

        #endregion Methods
    }
}