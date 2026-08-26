namespace NestLabs.Shared.Flow
{
    /// <summary>
    /// The single source of truth for flow state. Lives in Shared so any assembly can read it
    /// without pulling in the runtime, and so systems can gate on IsPlaying instead of each
    /// inventing their own "are we paused" flag.
    /// </summary>
    public interface IGameStateService
    {
        GameState Current { get; }

        /// <summary>Shorthand for the common gate: only Play ticks gameplay.</summary>
        bool IsPlaying { get; }

        /// <summary>Leaves a finished or paused run. Rejected from Play.</summary>
        void EnterMenu();

        /// <summary>Starts or restarts a run. Rejected from Play.</summary>
        void EnterPlay();

        /// <summary>Freezes the run. Rejected outside Play.</summary>
        void Pause();

        /// <summary>Unfreezes the run. Rejected outside Pause.</summary>
        void Resume();
    }
}
