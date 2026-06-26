using System.Collections.Generic;
using System.Linq;
using BearsShiftyEarth.Compat;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace BearsShiftyEarth
{
    /// <summary>
    /// Core mod system for Bear's Shifty Earth. Handles registration of ShiftyFalling block behavior.
    /// </summary>
    public class BearsShiftyEarthModSystem : ModSystem
    {
        #region Fields

        private bool[] solidOverrides = [];
        private ShiftySettings? config;
        private List<IModCompatHandler> friendMods = [];

        #endregion Fields

        #region Methods

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            ShiftySettingsSystem configSys = api.ModLoader.GetModSystem<ShiftySettingsSystem>();
            if (configSys == null) {
                Mod.Logger.Error(Lang.Get("bearsshiftyearth:settings-missing-error"));
                return;
            }

            config = configSys.Settings;

            // Not sure if this is how cross mod compatibility works!
            // Fuck it, we're doing it live!
            friendMods = Compatibility.GetFriendMods(config, api);

            // Only do this if the user wants soil instability in this world
            string blockGravity = api.World.Config.GetString("blockGravity", "sandgravel");
            if (blockGravity == "sandgravelsoil") {
                // We register our custom behavior under the default behavior's name to route the logic through us
                api.RegisterBlockBehaviorClass("UnstableFalling", typeof(BlockBehaviorShiftyFalling));
            }

            foreach (IModCompatHandler mod in friendMods) {
                mod.RegisterCompatBehaviors(api);
            }
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            if (config == null || api.Side != EnumAppSide.Server) {
                return;
            }

            // Only do this if the user wants soil instability in this world
            string blockGravity = api.World.Config.GetString("blockGravity", "sandgravel");
            if (blockGravity != "sandgravelsoil") {
                return;
            }

            // instantiate stuff for the loop
            solidOverrides = new bool[api.World.Blocks.Count];
            string blockCode;
            ConfigurationFactory shiftyFactory = new(config);

            foreach (Block block in api.World.Blocks) {
                // Check if we should enable the block logic for clay and farmland
                blockCode = block.Code.FirstCodePart();

                // first, add falling code if it's clay and configured for this
                if (blockCode is BlockCodes.CLAY_CODE && config.ClayBehavior == ShiftySettings.FallingBehaviorFlag.ShiftyEarth) {
                    // don't duplicate
                    if (block.HasBehavior<BlockBehaviorShiftyFalling>()) {
                        continue;
                    }
                    else {
                        AttachShiftyBehavior(block, shiftyFactory);
                    }
                }

                // also add falling code to farmland if it's configured
                else if (blockCode is BlockCodes.FARM_CODE && config.FarmlandBehavior == ShiftySettings.FallingBehaviorFlag.ShiftyEarth) {
                    if (block.HasBehavior<BlockBehaviorShiftyFalling>()) {
                        continue;
                    }
                    else {
                        AttachShiftyBehavior(block, shiftyFactory);
                    }
                }

                // finally, if it has a shifty behavior, configure it with the loaded settings
                BlockBehaviorShiftyFalling behavior = block.GetBehavior<BlockBehaviorShiftyFalling>();
                if (behavior != null) {
                    shiftyFactory.ConfigureBehavior(behavior);
                }

                // automatically add terrain slabs as solid for compatibility
                if (block.Code.Domain == "terrainslabs") {
                    solidOverrides[block.Id] = true;
                }
            }

            // iterate over the solidity overrides and cache them
            foreach (string solidOverride in config.SolidityOverrides) {
                Block[] blocks = api.World.SearchBlocks(solidOverride);
                if (blocks.Length > 0) {
                    foreach (Block block in blocks) {
                        solidOverrides[block.Id] = true;
                    }
                }
            }

            ShiftyUtil.Solids = solidOverrides;
            base.AssetsFinalize(api);
        }

        /// <summary>
        /// Creates a ShiftyFalling Block Behavior and manually attaches it to the block.
        /// </summary>
        private void AttachShiftyBehavior(Block block, ConfigurationFactory factory)
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
            BlockBehaviorShiftyFalling shiftyFallingBehavior = new(block);
            shiftyFallingBehavior.Initialize(shiftyProperties);

            // for some reason we have to configure the behavior here because it gets missed by the ConfigureBehavior call in the main thread.
            // apparently GetBehavior needs some kind of preregistration and it isn't catching the manually added ones.
            factory.ConfigureBehavior(shiftyFallingBehavior);

            List<BlockBehavior> behaviorsList = block.BlockBehaviors.ToList();
            behaviorsList.Add(shiftyFallingBehavior);
            block.BlockBehaviors = behaviorsList.ToArray();
        }
    }
}

        #endregion Methods