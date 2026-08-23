using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Hit points and invulnerability window. Owns the "can I be hurt right now" decision so no
    /// state has to duplicate the i-frame check.
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        private PlayerConfigSO _config;
        private float _invulnerableUntil;

        public int Current { get; private set; }

        public bool IsDead => Current <= 0;

        public bool Invulnerable => Time.time < _invulnerableUntil;

        /// <summary>Where the most recent accepted hit came from. The Hit state aims knockback away from it.</summary>
        public Vector2 LastDamageSourcePosition { get; private set; }

        /// <summary>Called by PlayerBase once the config has been injected.</summary>
        public void Initialize(PlayerConfigSO config)
        {
            _config = config;
            Current = config.MaxHealth;
            _invulnerableUntil = 0f;
        }

        /// <summary>
        /// Returns false — and changes nothing — while i-frames are active or the player is already
        /// dead. A true result means the caller should drive the FSM into Hit or Dead.
        /// </summary>
        public bool TryApplyDamage(int amount, Vector2 sourcePosition)
        {
            if (IsDead || Invulnerable || amount <= 0)
            {
                return false;
            }

            Current = Mathf.Max(0, Current - amount);
            LastDamageSourcePosition = sourcePosition;
            _invulnerableUntil = Time.time + _config.InvulnerabilityDuration;
            return true;
        }

        /// <summary>Knockback velocity for the last accepted hit, mirrored away from the source.</summary>
        public Vector2 GetKnockback()
        {
            float away = transform.position.x < LastDamageSourcePosition.x ? -1f : 1f;
            return new Vector2(_config.KnockbackVelocity.x * away, _config.KnockbackVelocity.y);
        }
    }
}
