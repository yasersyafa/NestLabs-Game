namespace NestLabs.Shared.Flow
{
    /// <summary>
    /// Swallows every transition and always reads as Play. Default when no container has injected
    /// a real one, so a prefab dropped into a bare scene still behaves as if a run were live.
    /// </summary>
    public sealed class NullGameStateService : IGameStateService
    {
        public static readonly NullGameStateService Instance = new NullGameStateService();

        public GameState Current => GameState.Play;
        public bool IsPlaying => true;

        public void EnterMenu() { }
        public void EnterPlay() { }
        public void Pause() { }
        public void Resume() { }
    }
}
