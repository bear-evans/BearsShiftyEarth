using System.Collections.Generic;
using System.Linq;
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

        public bool[] SolidOverrides { get; private set; } = [];

        #endregion Fields

        #region Methods

        #endregion Methods

        #region Fields

        private ShiftySettings? config;

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

            // Only do this if the user wants soil instability in this world
            string blockGravity = api.World.Config.GetString("blockGravity", "sandgravel");
            if (blockGravity == "sandgravelsoil") {
                // We register our custom behavior under the default behavior's name to route the logic through us
                api.RegisterBlockBehaviorClass("UnstableFalling", typeof(BlockBehaviorShiftyFalling));
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

            SolidOverrides = new bool[api.World.Blocks.Count];
            string blockCode;

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
                        AttachShiftyBehavior(block, config);
                    }
                }

                // also add falling code to farmland if it's configured
                else if (blockCode is BlockCodes.FARM_CODE && config.FarmlandBehavior == ShiftySettings.FallingBehaviorFlag.ShiftyEarth) {
                    if (block.HasBehavior<BlockBehaviorShiftyFalling>()) {
                        continue;
                    }
                    else {
                        AttachShiftyBehavior(block, config);
                    }
                }

                // finally, if it has a shifty behavior, configure it with the loaded settings
                block.GetBehavior<BlockBehaviorShiftyFalling>()?.ConfigureBehavior(config);

                // automatically add terrain slabs as solid for compatibility
                if (block.Code.Domain == "terrainslabs") {
                    SolidOverrides[block.Id] = true;
                }
                else if (config.SolidityOverrides.Contains(block.Code)) {
                }
            }

            base.AssetsFinalize(api);
        }

        public void GetClayJSON(ShiftySettings config)
        {
            JsonObject shiftyProperties = JsonObject.FromJson(@"{
                        ""requiredSupport"": 0,
                        ""adjacentSupport"": 0,
                        ""belowSupport"": 0,
                        ""topSupport"": 0,
                        ""rainPenalty"": 0,
                        ""plantBonus"": 0,
                        ""fallChance"": 0
                    }");

            // clay does not fall in vanilla
            if (config.ClayBehavior is ShiftySettings.FallingBehaviorFlag.Vanilla or ShiftySettings.FallingBehaviorFlag.Disabled) {
                DisableFalling(); // in case it somehow falls through the earlier behavior assignment
            }
            else {
                requiredSupport = config.ClaySupportRequired;
                adjacentSupport = config.ClayAdjacentSupport;
                belowSupport = config.ClayBelowSupport;
                topSupport = config.ClayAboveSupport;
                rainPenalty = config.MaximumClayStormPenalty;
                fallSideways = true;
                _ = SetFallChance(config.ClayFallChance);
            }
        }

        /// <summary>
        /// Creates a ShiftyFalling Block Behavior and manually attaches it to the block.
        /// </summary>
        private void AttachShiftyBehavior(Block block, ShiftySettings config)
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
            shiftyFallingBehavior.ConfigureBehavior(config);

            List<BlockBehavior> behaviorsList = block.BlockBehaviors.ToList();
            behaviorsList.Add(shiftyFallingBehavior);
            block.BlockBehaviors = behaviorsList.ToArray();
        }

        private void ConstructOverrides(ICoreAPI api)
        {
            if (config?.SolidityOverrides?.Count > 0) {
                Block? testBlock;
                for (int i = 0; i < config.SolidityOverrides.Count; i++) {
                    testBlock = api.World.GetBlock(new AssetLocation(config.SolidityOverrides[i]));
                    if (testBlock != null) {
                        SolidOverrides[testBlock.Id] = true;
                    }
                }
            }

            // automatically add terrain slabs if enabled
            if (api.ModLoader.IsModSystemEnabled("terrainslabs")) {
            }
        }
    }
}

        #endregion Methods