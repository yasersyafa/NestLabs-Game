using System;
using NestLabs.Shared.Combat;
using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Trigger collider that detects contact with an <see cref="IDamageSource"/>. It only reports —
    /// PlayerBase decides what the FSM does about it, so the hurtbox stays free of state knowledge.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerHurtbox : MonoBehaviour
    {
        [SerializeField] private Collider2D _trigger;

        /// <summary>Raised when a damage source is touched. PlayerBase subscribes.</summary>
        public event Action<IDamageSource> DamageDetected;

        private void Reset()
        {
            _trigger = GetComponent<Collider2D>();
            _trigger.isTrigger = true;
        }

        private void Awake()
        {
            if (_trigger == null) _trigger = GetComponent<Collider2D>();
            _trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // GetComponentInParent so a hazard can put its collider on a child of the object
            // that carries the IDamageSource script.
            if (other.GetComponentInParent<IDamageSource>() is IDamageSource source)
            {
                DamageDetected?.Invoke(source);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Stay is needed too: after i-frames expire while still standing in a hazard,
            // Enter has already fired and would never fire again.
            if (other.GetComponentInParent<IDamageSource>() is IDamageSource source)
            {
                DamageDetected?.Invoke(source);
            }
        }
    }
}
