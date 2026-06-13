namespace BearsShiftyEarth
{
    internal static class Constants
    {
        #region Properties

        public static string SoilWildcard { get => SOIL_CODE; }
        public static string ClayWildcard { get => CLAY_CODE; }
        public static string PeatWildcard { get => PEAT_CODE; }
        public static string SparseWildcard { get => SPARSE_GRASS_CODE; }
        public static string PatchyWildcard { get => PATCHY_GRASS_CODE; }
        public static string GrassyWildcard { get => GRASSY_GRASS_CODE; }

        #endregion Properties

        #region Fields

        private const string SOIL_CODE = "soil";
        private const string CLAY_CODE = "rawclay";
        private const string PEAT_CODE = "peat";
        private const string SPARSE_GRASS_CODE = "verysparse";
        private const string PATCHY_GRASS_CODE = "sparse";
        private const string GRASSY_GRASS_CODE = "normal";

        #endregion Fields
    }
}