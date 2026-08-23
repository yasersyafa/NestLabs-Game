using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Kinematic movement. Owns velocity and gravity itself rather than handing them to the
    /// Physics2D solver, so a wall jump traces the exact same arc every single tap.
    /// States write <see cref="Velocity"/>; only this class moves the transform.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        private const int MaxSlideIterations = 3;

        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private Collider2D _collider;

        [Tooltip("Layers the motor collides against. Hazards are trigger-only and must NOT be here.")]
        [SerializeField] private LayerMask _solidLayers;

        [Tooltip("Gap kept between the collider and a surface so the next cast never starts overlapping.")]
        [SerializeField] private float _skinWidth = 0.02f;

        // Preallocated: Rigidbody2D.Cast into a buffer never allocates, unlike Physics2D.BoxCastAll.
        private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];
        private ContactFilter2D _filter;

        /// <summary>Current velocity in units/sec. States set this; Move consumes it.</summary>
        public Vector2 Velocity { get; set; }

        /// <summary>What the last <see cref="Move"/> resolve ended up touching.</summary>
        public PlayerCollisionFlags Flags { get; private set; }

        /// <summary>Multiplier on config gravity. Dash sets 0, normal states set 1.</summary>
        public float GravityScale { get; private set; } = 1f;

        public Rigidbody2D Body => _body;
        public Collider2D Collider => _collider;
        public float SkinWidth => _skinWidth;
        public ContactFilter2D Filter => _filter;

        private void Reset()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        private void Awake()
        {
            if (_body == null) _body = GetComponent<Rigidbody2D>();
            if (_collider == null) _collider = GetComponent<Collider2D>();

            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.simulated = true;

            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _solidLayers,
                useTriggers = false
            };
        }

        public void SetGravityScale(float scale)
        {
            GravityScale = scale;
        }

        /// <summary>Zeroes velocity and collision flags. Called on respawn.</summary>
        public void ResetMotion()
        {
            Velocity = Vector2.zero;
            Flags = PlayerCollisionFlags.None;
            GravityScale = 1f;
        }

        /// <summary>
        /// Accelerates <see cref="Velocity"/> downward by gravity * <see cref="GravityScale"/>,
        /// clamped to <paramref name="maxFallSpeed"/>.
        /// </summary>
        public void ApplyGravity(float gravity, float maxFallSpeed, float dt)
        {
            if (GravityScale <= 0f)
            {
                return;
            }

            Vector2 v = Velocity;
            v.y -= gravity * GravityScale * dt;

            if (v.y < -maxFallSpeed)
            {
                v.y = -maxFallSpeed;
            }

            Velocity = v;
        }

        /// <summary>
        /// Sweeps the collider along Velocity * dt. On contact it stops at the surface minus
        /// <see cref="_skinWidth"/>, strips the velocity component pointing into that surface, and
        /// slides the leftover distance along it. Updates <see cref="Flags"/>.
        /// </summary>
        public void Move(float dt)
        {
            Flags = PlayerCollisionFlags.None;

            Vector2 remaining = Velocity * dt;

            for (int i = 0; i < MaxSlideIterations; i++)
            {
                float distance = remaining.magnitude;
                if (distance <= Mathf.Epsilon)
                {
                    break;
                }

                Vector2 direction = remaining / distance;
                int count = _body.Cast(direction, _filter, _hitBuffer, distance + _skinWidth);

                if (count == 0)
                {
                    _body.position += remaining;
                    break;
                }

                // Cast does not guarantee ordering, so pick the nearest hit explicitly.
                RaycastHit2D nearest = _hitBuffer[0];
                for (int h = 1; h < count; h++)
                {
                    if (_hitBuffer[h].distance < nearest.distance)
                    {
                        nearest = _hitBuffer[h];
                    }
                }

                float travel = Mathf.Max(0f, nearest.distance - _skinWidth);
                _body.position += direction * travel;

                RecordContact(nearest.normal);

                // Kill the component of both velocity and leftover motion that points into the wall.
                Velocity -= nearest.normal * Vector2.Dot(Velocity, nearest.normal);

                remaining -= direction * travel;
                remaining -= nearest.normal * Vector2.Dot(remaining, nearest.normal);
            }
        }

        private void RecordContact(Vector2 normal)
        {
            if (normal.y > 0.5f)
            {
                Flags |= PlayerCollisionFlags.Grounded;
            }
            else if (normal.y < -0.5f)
            {
                Flags |= PlayerCollisionFlags.Ceiling;
            }
            else if (normal.x > 0.5f)
            {
                // Normal points right, so the surface is on the player's left.
                Flags |= PlayerCollisionFlags.WallLeft;
            }
            else if (normal.x < -0.5f)
            {
                Flags |= PlayerCollisionFlags.WallRight;
            }
        }
    }
}
