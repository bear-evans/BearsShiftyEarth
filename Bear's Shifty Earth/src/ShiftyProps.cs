namespace BearsShiftyEarth
{
    /// <summary>
    /// A struct containing all configured properties related to Shifty Earth logic.
    /// </summary>
    public struct ShiftyProps
    {
        #region Fields

        public bool permStable = false;
        public int requiredSupport = 10;
        public int adjacentSupport = 0;
        public int belowSupport = 0;
        public int aboveSupport = 0;
        public int rainPenalty = 0;
        public int plantBonus = 0;

        public ShiftyProps(bool stable = false, int required = 10, int adjacent = 0, int below = 0, int above = 0, int rain = 0, int plant = 0)
        {
            permStable = stable;
            requiredSupport = required;
            adjacentSupport = adjacent;
            belowSupport = below;
            aboveSupport = above;
            rainPenalty = rain;
            plantBonus = plant;
        }

        #endregion Fields
    }
}