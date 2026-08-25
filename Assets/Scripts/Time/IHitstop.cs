namespace NestLabs
{
    /// <summary>
    /// Brief global time-scale dip, used to sell an impact or a wind-up. States talk to this rather
    /// than to Time.timeScale directly, so nothing can strand the game at half speed when a state
    /// is cut short by a death or a scene reload.
    /// </summary>
    public interface IHitstop
    {
        /// <summary>
        /// Drops the time scale for <paramref name="unscaledDuration"/> real seconds. A second call
        /// while one is running retargets it rather than stacking.
        /// </summary>
        void Begin(float scale, float unscaledDuration);

        /// <summary>Restores full speed immediately.</summary>
        void Cancel();
    }
}
