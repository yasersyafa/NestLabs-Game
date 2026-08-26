namespace NestLabs.Shared.Flow
{
    /// <summary>
    /// The whole game's flow state. Distinct from PlayerStateId, which only describes what the
    /// player is doing inside a run.
    /// </summary>
    public enum GameState
    {
        /// <summary>Pre-init only. No service ever reports this once it is constructed.</summary>
        None = 0,
        Menu,
        Play,
        Pause,
        Death
    }
}
