using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace BearsShiftyEarth
{
    /// <summary>
    /// Core mod system for Bear's Shifty Earth. Handles registration of ShiftyFalling block behavior.
    /// </summary>
    public class BearsShiftyEarthModSystem : ModSystem
    {
        #region Enums

        #endregion Enums

        #region Properties

        public static ICoreAPI? Api { get => API; }
        public static ILogger? Logger { get => LOGGER; }

        #endregion Properties

        #region Fields

        private static ICoreAPI? API;
        private static ILogger? LOGGER;

        #endregion Fields

        #region Methods

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            // save references for use by the block behavior
            API = api;
            LOGGER = Mod.Logger;

            // We register our custom behavior under the default behavior's name to route the logic through us
            api.RegisterBlockBehaviorClass("UnstableFalling", typeof(BlockBehaviorShiftyFalling));
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            string blockCode;
            foreach (Block block in api.World.Blocks) {
                // Check if we should enable the block logic for clay and farmland

                blockCode = block.Code.FirstCodePart();

                if (blockCode is BlockCodes.CLAY_CODE && ShiftySettingsSystem.Settings.ClayBehavior == ShiftySettings.FallingBehaviorFlag.ShiftyEarth) {
                    // don't duplicate
                    if (block.HasBehavior<BlockBehaviorShiftyFalling>()) {
                        continue;
                    }
                    else {
                        AttachShiftyBehavior(block);
                    }
                }
                else if (blockCode is BlockCodes.FARM_CODE && ShiftySettingsSystem.Settings.ClayBehavior == ShiftySettings.FallingBehaviorFlag.ShiftyEarth) {
                    if (block.HasBehavior<BlockBehaviorShiftyFalling>()) {
                        continue;
                    }
                    else {
                        AttachShiftyBehavior(block);
                    }
                }
            }
            base.AssetsFinalize(api);
        }

        /// <summary>
        /// Creates a ShiftyFalling Block Behavior and manually attaches it to the block.
        /// </summary>
        private void AttachShiftyBehavior(Block block)
        {
            // these don't actually mean anything right now, it just wanted a JSON and I didn't feel
            // like rewriting all the in-behavior
            // configuration logic to appease the machine. It's here in case I want to for mod compatibility.
            JsonObject shiftyProperties = JsonObject.FromJson(@"{
                        ""requiredSupport"": 8,
                        ""adjacentSupport"": 5,
                        ""belowSupport"": 15,
                        ""topSupport"": 5
                    }");

            // manually add the block behavior if configured to do so
            BlockBehavior shiftyFallingBehavior = new BlockBehaviorShiftyFalling(block);
            shiftyFallingBehavior.Initialize(shiftyProperties);

            List<BlockBehavior> behaviorsList = block.BlockBehaviors.ToList();
            behaviorsList.Add(shiftyFallingBehavior);
            block.BlockBehaviors = behaviorsList.ToArray();
        }

        #endregion Methods
    }
}