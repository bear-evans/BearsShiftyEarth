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
        public static AssetLocation SoilWildcard { get => SOILWILDCARD; }
        public static AssetLocation ClayWildcard { get => CLAYWILDCARD; }
        public static AssetLocation PeatWildcard { get => PEATWILDCARD; }

        #endregion Properties

        #region Fields

        private static ICoreAPI? API;
        private static ILogger? LOGGER;

        private static AssetLocation SOILWILDCARD = new("soil-*-*");
        private static AssetLocation CLAYWILDCARD = new("rawclay-*-*");
        private static AssetLocation PEATWILDCARD = new("peat-*-*");

        private static AssetLocation SPARSEGRASSWILDCARD = new("*-*-verysparse");
        private static AssetLocation PATCHYGRASSWILDCARD = new("*-*-*");
        private static AssetLocation GRASSYWILDCARD = new("*-*-grassy");

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