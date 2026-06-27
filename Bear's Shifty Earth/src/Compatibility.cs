using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        /// <summary>
        /// Searches for mods that require special compatibility code and returns references to their compat handlers.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<IModCompatHandler> GetFriendMods(ShiftySettings config, ICoreAPI api)
        {
            List<IModCompatHandler> friends = [];

            Mod mod = api.ModLoader.GetMod("terrainslabs");
            if (mod != null) {
                IModCompatHandler? tslabHandler = LoadTerrainSlabsCompatAssembly(api);
                if (tslabHandler != null) { friends.Add(tslabHandler); }
            }

            return friends;
        }

        /// <summary>
        /// Dynamically loads an embedded assembly containing a TerrainSlabs version of BlockBehaviorShiftyFalling.
        /// </summary>
        private static IModCompatHandler? LoadTerrainSlabsCompatAssembly(ICoreAPI api)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

#if DEBUG
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames) {
                api.Logger.Debug(resourceName);
            }
#endif

            using Stream? stream = assembly.GetManifestResourceStream("BearsShiftyEarth.ShiftyEarthCompat.dll");
            if (stream == null) {
                api.Logger.Warning("Terrain Slabs detected, but Shifty Earth was unable to load the compatibility DLL.");
                return null;
            }
            byte[] assemblyData = new byte[stream.Length];
            _ = stream.Read(assemblyData, 0, assemblyData.Length);

            Assembly compatAssembly = Assembly.Load(assemblyData);

            Type? handlerType = compatAssembly.GetType("BearsShiftyEarth.Compat.TerrainSlabsCompat");
            if (handlerType == null) {
                api.Logger.Warning($"Failed to find TerrainSlabsCompat type.");
                return null;
            }

            api.Logger.Warning("Creating instance of Compat handler!");
            if (Activator.CreateInstance(handlerType) is not IModCompatHandler handler) {
                api.Logger.Warning($"Failed to create an instance of {handlerType}");
                return null;
            }

            api.Logger.Warning("Compat loading successful!");
            return handler;
        }

        #endregion Methods
    }
}