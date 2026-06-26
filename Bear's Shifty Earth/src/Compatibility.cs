using System.Collections.Generic;
using Vintagestory.API.Common;

namespace BearsShiftyEarth
{
    public class Compatibility
    {
        #region Methods

        public static List<IModCompatHandler> GetFriendMods(ShiftySettings config, ICoreAPI api)
        {
            List<IModCompatHandler> friends = [];

            Mod mod = api.ModLoader.GetMod("terrainslabs");
            if (mod != null) {
                friends.Add(new TerrainSlabsCompat(config, mod));
            }

            return friends;
        }

        #endregion Methods

        #region Classes

        /// <summary>
        /// Interface for all mod compat objects so they are never directly referenced by Shifty Earth main modules.
        /// </summary>
        public interface IModCompatHandler
        {
            #region Properties

            Mod? FriendMod { get; }

            #endregion Properties

            #region Methods

            void RegisterCompatBehaviors();

            void AttachBehaviorsToBlocks(ShiftySettings config, ICoreAPI api);

            void ConfigureCompatBlocks(ShiftySettings config, ConfigurationFactory factory);

            #endregion Methods
        }

        /// <summary>
        /// Object that handles compatibility between Shifty Earth and terrain slabs.
        /// </summary>
        public class TerrainSlabsCompat : IModCompatHandler
        {
            #region Properties

            public Mod? FriendMod { get => modRef; }

            #endregion Properties

            #region Constructors

            public TerrainSlabsCompat(ShiftySettings config, Mod terrainslabs)
            {
            }

            #endregion Constructors

            #region Methods

            public void AttachBehaviorsToBlocks(ShiftySettings config, ICoreAPI api)
            {
                throw new System.NotImplementedException();
            }

            public void ConfigureCompatBlocks(ShiftySettings config, ConfigurationFactory factory)
            {
                throw new System.NotImplementedException();
            }

            public void RegisterCompatBehaviors()
            {
                throw new System.NotImplementedException();
            }

            #endregion Methods

            #region Fields

            private Mod? modRef;

            #endregion Fields
        }

        #endregion Classes
    }
}