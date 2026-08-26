using NestLabs.Node;
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
        public PlayerNodeSensor NodeSensor { get; }
        public PlayerVisual Visual { get; }
        public PlayerTrail Trail { get; }
        public PlayerHealth Health { get; }
        /// <summary>
        /// Resolved on every read rather than once in the constructor. PlayerBase builds this in
        /// Start, and a hierarchy tool can destroy a service in Start too, with no ordering
        /// guarantee between them. A construction-time check would catch that only sometimes.
        /// </summary>
        public IHitstop Hitstop => NullHitstop.Safe(_hitstop);

        private readonly IHitstop _hitstop;
        public PlayerConfigSO Config { get; }
        public IPlayerInput Input { get; }
        public IPlayerEventSink Events { get; }
        public PlayerStateMachine Fsm { get; }
        public Transform Transform { get; }

        public PlayerContext(
            PlayerStateMachine fsm,
            PlayerMotor motor,
            PlayerSensor sensor,
            PlayerNodeSensor nodeSensor,
            PlayerVisual visual,
            PlayerTrail trail,
            PlayerHealth health,
            IHitstop hitstop,
            PlayerConfigSO config,
            IPlayerInput input,
            IPlayerEventSink events,
            Transform transform)
        {
            Fsm = fsm;
            Motor = motor;
            Sensor = sensor;
            NodeSensor = nodeSensor;
            Visual = visual;
            Trail = trail;
            Health = health;
            _hitstop = hitstop;
            Config = config;
            Input = input;
            Events = events;
            Transform = transform;
        }

        /// <summary>Shorthand for the sensor's latest probe.</summary>
        public PlayerSense Sense => Sensor.Current;

        // --- Blackboard -------------------------------------------------------------------

        /// <summary>The node the current launch is aimed at. Set on tap, cleared when Dash exits.</summary>
        public NodeBase ActiveNode { get; set; }

        /// <summary>Seconds of launch-speed bleed-off left. Armed by Dash on exit, spent by Fall.</summary>
        public float GrappleDecayRemaining { get; set; }

        /// <summary>How fast the bleed-off runs, in units/sec^2. Derived from the launch speed.</summary>
        public float GrappleDecayRate { get; set; }

        /// <summary>Arms the post-launch bleed-off so the pull settles into normal air speed.</summary>
        public void ArmGrappleDecay(float launchSpeed)
        {
            float duration = Config.GrappleExitDecayDuration;
            float excess = Mathf.Abs(launchSpeed) - Config.GrappleExitSpeed;

            if (duration <= 0f || excess <= 0f)
            {
                GrappleDecayRemaining = 0f;
                return;
            }

            GrappleDecayRemaining = duration;
            GrappleDecayRate = excess / duration;
        }

        /// <summary>-1 or +1. Drives sprite flip.</summary>
        public int FacingDirection { get; set; } = 1;

        /// <summary>Side of the wall the player was last attached to: -1 left, +1 right.</summary>
        public int LastWallSide { get; set; }

        /// <summary>When the player last stopped touching a wall. Feeds coyote time.</summary>
        public float LastWallExitTime { get; set; } = float.NegativeInfinity;

        /// <summary>True while a tap should still be treated as a wall jump despite being airborne.</summary>
        public bool HasCoyoteWall =>
            LastWallSide != 0 && Time.time - LastWallExitTime <= Config.CoyoteDuration;

        /// <summary>
        /// Nearest ready grapple node the player is inside. The only gate on launching: no node in
        /// range means an airborne tap does nothing.
        /// </summary>
        public bool TryGetNodeInRange(out NodeBase node)
        {
            if (NodeSensor == null)
            {
                node = null;
                return false;
            }

            return NodeSensor.TryGetNearest(Transform.position, out node);
        }

        /// <summary>Wipes per-life blackboard values. Called on spawn and respawn.</summary>
        public void ResetBlackboard()
        {
            ActiveNode = null;
            GrappleDecayRemaining = 0f;
            GrappleDecayRate = 0f;
            FacingDirection = 1;
            LastWallSide = 0;
            LastWallExitTime = float.NegativeInfinity;
        }
    }
}
