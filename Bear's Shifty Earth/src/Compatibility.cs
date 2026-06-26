using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;

namespace BearsShiftyEarth.Compat
{
    /// <summary>
    /// Interface for all mod compat objects so they are never directly referenced by Shifty Earth main modules.
    /// </summary>
    public interface IModCompatHandler
    {
        #region Properties

        Mod? FriendMod { get; }

        #endregion Properties

        #region Methods

        void RegisterCompatBehaviors(ICoreAPI api);

        void AttachBehaviorsToBlocks(ShiftySettings config, ICoreAPI api);

        void ConfigureCompatBlocks(ShiftySettings config, ConfigurationFactory factory);

        #endregion Methods
    }

    public class Compatibility
    {
        #region Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<IModCompatHandler> GetFriendMods(ShiftySettings config, ICoreAPI api)
        {
            List<IModCompatHandler> friends = [];

            Mod mod = api.ModLoader.GetMod("terrainslabs");
            if (mod != null) {
                //friends.Add(new TerrainSlabsCompat(config, mod));
            }

            return friends;
        }

        #endregion Methods
    }
}