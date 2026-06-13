using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace BearsShiftyEarth
{
    /// <summary>
    /// Core mod system for Bear's Shifty Earth. Handles registration of ShiftyFalling block behavior.
    /// </summary>
    public class BearsShiftyEarthModSystem : ModSystem
    {
        #region Enums

        public enum EarthType
        {
            NONE,
            Soil,
            Clay,
            Peat
        }

        #endregion Enums

        #region Properties

        public static ICoreAPI? Api { get => API; }
        public static ILogger? Logger { get => LOGGER; }
        public static string SoilWildcard { get => SOILWILDCARD; }
        public static string ClayWildcard { get => CLAYWILDCARD; }
        public static string PeatWildcard { get => PEATWILDCARD; }
        public static string SparseWildcard { get => SPARSEGRASSWILDCARD; }
        public static string PatchyWildcard { get => PATCHYGRASSWILDCARD; }
        public static string GrassyWildcard { get => GRASSYWILDCARD; }

        #endregion Properties

        #region Fields

        private const string SOILWILDCARD = "soil";
        private const string CLAYWILDCARD = "rawclay";
        private const string PEATWILDCARD = "peat";
        private const string SPARSEGRASSWILDCARD = "verysparse";
        private const string PATCHYGRASSWILDCARD = "sparse";
        private const string GRASSYWILDCARD = "normal";
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

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification(Lang.Get("bearsshiftyearth:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification(Lang.Get("bearsshiftyearth:hello"));
        }

        #endregion Methods
    }
}