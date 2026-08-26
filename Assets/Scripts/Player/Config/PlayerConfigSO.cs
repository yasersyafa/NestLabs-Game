using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Every tuning number the player uses. Lives in an asset so designers retune without a
    /// recompile, and so an A/B feel test is just a second asset on a second prefab.
    /// No logic belongs here — this is data only.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "NestLabs/Player/Player Config")]
    public sealed class PlayerConfigSO : ScriptableObject
    {
        [Header("Gravity")]
        [Tooltip("Downward acceleration in units/sec^2. Applied by the motor, not by Physics2D.")]
        public float Gravity = 60f;

        [Tooltip("Terminal velocity while airborne, in units/sec.")]
        public float MaxFallSpeed = 25f;

        [Header("Jump")]
        [Tooltip("Upward velocity applied the instant the player leaves a wall.")]
        public float JumpVelocity = 18f;

        [Tooltip("Horizontal velocity away from the wall. Together with Gravity this fixes the arc width.")]
        public float JumpHorizontalSpeed = 9f;

        [Tooltip("Vertical velocity multiplier applied at apex cut. 1 = no cut.")]
        [Range(0f, 1f)]
        public float JumpCutMultiplier = 0.5f;

        [Header("Grapple")]
        [Tooltip("Safety cap on a pull, in seconds. Only reached when geometry blocks the path to the node. Speed and range come from the node's own NodeDataSO.")]
        public float GrappleMaxDuration = 0.5f;

        [Tooltip("Seconds the player hangs motionless before the launch fires. Real time, so the slow-mo does not stretch it.")]
        [Min(0f)] public float GrappleAnticipationDuration = 0.08f;

        [Tooltip("Time scale during the wind-up. Lasts exactly GrappleAnticipationDuration. 1 disables the slow-mo entirely.")]
        [Range(0.05f, 1f)] public float GrappleTimeScale = 0.5f;

        [Tooltip("Horizontal speed the launch bleeds back down to after crossing the node.")]
        [Min(0f)] public float GrappleExitSpeed = 9f;

        [Tooltip("Seconds to blend from launch speed down to GrappleExitSpeed.")]
        [Min(0f)] public float GrappleExitDecayDuration = 0.35f;

        [Header("Latch")]
        [Tooltip("Seconds the player clings motionless before Slide takes over.")]
        public float LatchGraceDuration = 0.35f;

        [Tooltip("Inward velocity held against the wall so the sensor keeps reporting contact.")]
        public float LatchStickForce = 2f;

        [Header("Slide")]
        [Tooltip("Descent speed the moment Slide begins.")]
        public float SlideSpeed = 3f;

        [Tooltip("How fast the slide accelerates toward SlideMaxSpeed, in units/sec^2.")]
        public float SlideAcceleration = 8f;

        public float SlideMaxSpeed = 10f;

        [Header("Get Hit")]
        public int MaxHealth = 3;

        [Tooltip("Seconds of control lock after taking a hit.")]
        public float HitStunDuration = 0.3f;

        [Tooltip("Velocity applied away from the damage source.")]
        public Vector2 KnockbackVelocity = new Vector2(8f, 10f);

        [Tooltip("Seconds of immunity after a hit lands. Must be >= HitStunDuration or the player can be chain-hit while stunned.")]
        public float InvulnerabilityDuration = 1f;

        [Header("Input Feel")]
        [Tooltip("A tap fired this many seconds before wall contact still triggers the jump on Latch entry.")]
        public float InputBufferDuration = 0.12f;

        [Tooltip("A tap fired this many seconds after leaving a wall still counts as a wall jump.")]
        public float CoyoteDuration = 0.1f;

        private void OnValidate()
        {
            // The one invariant worth enforcing in data: stun must never outlast immunity,
            // or a hazard can re-hit a player who cannot yet act.
            if (InvulnerabilityDuration < HitStunDuration)
            {
                InvulnerabilityDuration = HitStunDuration;
            }

        }
    }
}
