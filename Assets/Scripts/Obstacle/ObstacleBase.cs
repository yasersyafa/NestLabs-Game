using NestLabs.Shared.Combat;
using NestLabs.Shared.Obstacle;
using UnityEngine;
using VContainer;

namespace Nestlabs.Obstacle
{
    /// <summary>
    /// Base for every obstacle. Implementing IDamageSource is what lets the player hurtbox find an
    /// obstacle without knowing its concrete type; OnHit comes back only once the hit is accepted.
    /// </summary>
    public abstract class ObstacleBase : MonoBehaviour, IHittable, IDamageSource
    {
        private IObstacleEventSink _eventSink = NullObstacleEventSink.Instance;

        // Override in a subclass for a heavier obstacle.
        public virtual int Damage => 1;

        public Vector2 Position => transform.position;

        [Inject]
        public void Construct(IObstacleEventSink eventSink)
        {
            _eventSink = eventSink;
        }

        public virtual void OnHit()
        {
            _eventSink.Hit(transform.position);
        }
    }
}
