using NestLabs.Player;
using UnityEngine;

namespace NestLabs
{
    /// <summary>
    /// Minimal <see cref="IDamageSource"/> for exercising the GetHit path in a test scene. Real
    /// hazards implement the same interface — PlayerHurtbox never learns their concrete types.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DebugHazard : MonoBehaviour, IDamageSource
    {
        [SerializeField] private int _damage = 1;

        public int Damage => _damage;

        public Vector2 Position => transform.position;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }
    }
}
