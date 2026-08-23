namespace NestLabs.Player
{
    /// <summary>
    /// The whole player input surface: one contextual tap. What the tap *means* is decided by the
    /// active state, never here. An interface so tests can drive the FSM without a device.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>When false, taps are still recorded by the device but never reported.</summary>
        bool Enabled { get; set; }

        /// <summary>
        /// True when an unconsumed tap arrived within <paramref name="bufferDuration"/> seconds.
        /// The buffer is what lets a tap fired slightly before wall contact still become a jump.
        /// </summary>
        bool HasBufferedTap(float bufferDuration);

        /// <summary>Marks the pending tap as used so it cannot trigger a second action.</summary>
        void ConsumeTap();

        /// <summary>Drops any pending tap. Called on death and on state-machine reset.</summary>
        void ClearTap();
    }
}
