namespace NestLabs.Player
{
    /// <summary>
    /// One exclusive locomotion behaviour. Implementations are plain C# classes allocated once at
    /// startup and reused for the lifetime of the player, so state changes never allocate.
    /// </summary>
    public interface IPlayerState
    {
        PlayerStateId Id { get; }

        void Enter();

        void Exit();

        /// <summary>Per-frame: timers and transition checks.</summary>
        void Tick(float deltaTime);

        /// <summary>Per-fixed-step: the only place a state writes to the motor.</summary>
        void FixedTick(float fixedDeltaTime);

        /// <summary>
        /// A buffered tap is pending. The state decides what a tap means here and calls
        /// ConsumeTap if it acts. Leaving it unconsumed keeps it buffered for the next state.
        /// </summary>
        void OnTap();
    }
}
