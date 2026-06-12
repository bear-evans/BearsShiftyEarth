using Vintagestory.API.Config;

namespace BearsShiftyEarth
{
    /// <summary>
    /// Stores a single block configuration.
    /// </summary>
    public class ShiftyBlock
    {
        #region Properties

        public string FallSidewaysComment { get => Lang.Get("bearshiftyearth:config-fallsideways"); }
        public bool CanFallSideways { get; set; }
        public float FallSidewaysChance { get; set; }
        public string RequireBeneathComment { get => Lang.Get("bearshiftyearth:config-requirebeneath"); }
        public int RequireBlockBeneath { get; set; }
        public int SupportTotalNeeded { get; set; }

        #endregion Properties
    }
}