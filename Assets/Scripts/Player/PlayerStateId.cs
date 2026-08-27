namespace NestLabs.Player
{
    /// <summary>
    /// Every locomotion state the player FSM can occupy. States are mutually exclusive:
    /// the player is in exactly one of these at any time.
    /// </summary>
    public enum PlayerStateId
    {
        /// <summary>No state yet. Only valid as the "from" value of the first state change.</summary>
        None = 0,

        /// <summary>Stuck to a wall, not descending. Grace timer running before Slide takes over.</summary>
        Latch,

        /// <summary>Descending the wall under gravity-lite. The time pressure of the climb.</summary>
        Slide,

        /// <summary>Launched off a wall, ascending toward the opposite wall.</summary>
        Jump,

        /// <summary>Descending through the air, post-apex or post-dash.</summary>
        Fall,

        /// <summary>Timed horizontal burst. Gravity suspended, input ignored.</summary>
        Dash,

        /// <summary>Knockback plus i-frames plus control lock.</summary>
        Hit,

        /// <summary>Terminal. Input detached.</summary>
        Dead,

        /// <summary>Pre-run. Resting on the floor, waiting for the first tap to start the climb.</summary>
        Idle
    }
}
