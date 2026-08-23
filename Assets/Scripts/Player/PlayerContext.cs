using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Everything a state is allowed to touch, handed over once at construction. This is why no
    /// state ever calls GetComponent: the wiring happens in exactly one place, PlayerBase.Awake.
    /// Also carries the small blackboard that outlives any single state (dash charges, facing,
    /// coyote bookkeeping).
    /// </summary>
    public sealed class PlayerContext
    {
        public PlayerMotor Motor { get; }
        public PlayerSensor Sensor { get; }
        public PlayerVisual Visual { get; }
        public PlayerHealth Health { get; }
        public PlayerConfigSO Config { get; }
        public IPlayerInput Input { get; }
        public IPlayerEventSink Events { get; }
        public PlayerStateMachine Fsm { get; }
        public Transform Transform { get; }

        public PlayerContext(
            PlayerStateMachine fsm,
            PlayerMotor motor,
            PlayerSensor sensor,
            PlayerVisual visual,
            PlayerHealth health,
            PlayerConfigSO config,
            IPlayerInput input,
            IPlayerEventSink events,
            Transform transform)
        {
            Fsm = fsm;
            Motor = motor;
            Sensor = sensor;
            Visual = visual;
            Health = health;
            Config = config;
            Input = input;
            Events = events;
            Transform = transform;
        }

        /// <summary>Shorthand for the sensor's latest probe.</summary>
        public PlayerSense Sense => Sensor.Current;

        // --- Blackboard -------------------------------------------------------------------

        /// <summary>Dashes left this airtime. Refilled on entering Latch.</summary>
        public int DashChargesRemaining { get; set; }

        /// <summary>-1 or +1. Drives sprite flip and the dash direction.</summary>
        public int FacingDirection { get; set; } = 1;

        /// <summary>Side of the wall the player was last attached to: -1 left, +1 right.</summary>
        public int LastWallSide { get; set; }

        /// <summary>When the player last stopped touching a wall. Feeds coyote time.</summary>
        public float LastWallExitTime { get; set; } = float.NegativeInfinity;

        public float LastDashTime { get; set; } = float.NegativeInfinity;

        /// <summary>True while a tap should still be treated as a wall jump despite being airborne.</summary>
        public bool HasCoyoteWall =>
            LastWallSide != 0 && Time.time - LastWallExitTime <= Config.CoyoteDuration;

        /// <summary>Charge and cooldown gate for the dash.</summary>
        public bool CanDash =>
            DashChargesRemaining > 0 && Time.time - LastDashTime >= Config.DashCooldown;

        /// <summary>Wipes per-life blackboard values. Called on spawn and respawn.</summary>
        public void ResetBlackboard()
        {
            DashChargesRemaining = Config.DashChargesPerAirtime;
            FacingDirection = 1;
            LastWallSide = 0;
            LastWallExitTime = float.NegativeInfinity;
            LastDashTime = float.NegativeInfinity;
        }
    }
}
