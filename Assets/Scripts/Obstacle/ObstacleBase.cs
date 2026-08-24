using NestLabs.Shared.Obstacle;
using UnityEngine;
using VContainer;

namespace Nestlabs.Obstacle
{
    public interface IHittable
    {
        void OnHit();
    }

    public abstract class ObstacleBase : MonoBehaviour, IHittable
    {
        private IObstacleEventSink _eventSink = NullObstacleEventSink.Instance;

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
