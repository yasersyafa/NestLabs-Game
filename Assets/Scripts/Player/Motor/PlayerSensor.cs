using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Probes the world around the player once per fixed step and publishes the result as an
    /// immutable <see cref="PlayerSense"/>. States ask this what is nearby; they never cast rays.
    /// </summary>
    public sealed class PlayerSensor : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;

        [Tooltip("Layers that count as a latchable wall or as ground.")]
        [SerializeField] private LayerMask _solidLayers;

        [Tooltip("How far ahead a wall is detected. Larger values make latching more forgiving.")]
        [SerializeField] private float _wallProbeDistance = 0.08f;

        [SerializeField] private float _groundProbeDistance = 0.08f;

        // Same reasoning as PlayerMotor: buffer + filter, so probing is allocation-free.
        private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[4];
        private ContactFilter2D _filter;

        /// <summary>
        /// Latest probe result. Refreshed by <see cref="Probe"/>. The setter is internal so EditMode
        /// tests can stage wall contact without building real colliders.
        /// </summary>
        public PlayerSense Current { get; internal set; } = PlayerSense.Nothing;

        private void Reset()
        {
            _motor = GetComponent<PlayerMotor>();
        }

        private void Awake()
        {
            if (_motor == null) _motor = GetComponent<PlayerMotor>();

            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _solidLayers,
                useTriggers = false
            };
        }

        /// <summary>Re-probes and refreshes <see cref="Current"/>. Called once per FixedUpdate by PlayerBase.</summary>
        public void Probe()
        {
            Rigidbody2D body = _motor.Body;
            if (body == null)
            {
                return;
            }

            int wallSide = 0;
            float wallDistance = float.PositiveInfinity;
            PlayerCollisionFlags flags = PlayerCollisionFlags.None;

            if (TryProbe(body, Vector2.right, _wallProbeDistance, out float rightDistance))
            {
                flags |= PlayerCollisionFlags.WallRight;
                wallSide = 1;
                wallDistance = rightDistance;
            }

            if (TryProbe(body, Vector2.left, _wallProbeDistance, out float leftDistance))
            {
                flags |= PlayerCollisionFlags.WallLeft;

                // Wedged between two walls: whichever is closer wins.
                if (leftDistance < wallDistance)
                {
                    wallSide = -1;
                    wallDistance = leftDistance;
                }
            }

            bool grounded = TryProbe(body, Vector2.down, _groundProbeDistance, out _);
            if (grounded)
            {
                flags |= PlayerCollisionFlags.Grounded;
            }

            Current = new PlayerSense(wallSide, grounded, wallDistance, flags);
        }

        private bool TryProbe(Rigidbody2D body, Vector2 direction, float distance, out float hitDistance)
        {
            int count = body.Cast(direction, _filter, _hitBuffer, distance);
            if (count == 0)
            {
                hitDistance = float.PositiveInfinity;
                return false;
            }

            hitDistance = _hitBuffer[0].distance;
            for (int i = 1; i < count; i++)
            {
                if (_hitBuffer[i].distance < hitDistance)
                {
                    hitDistance = _hitBuffer[i].distance;
                }
            }

            return true;
        }
    }
}
