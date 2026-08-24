using MessagePipe;
using NestLabs.Shared.Obstacle;
using UnityEngine;

namespace NestLabs
{
    /// <summary>
    /// Forwards obstacle notifications onto MessagePipe. The single point where the obstacle
    /// system touches the messaging library — mirrors MessagePipePlayerEventSink.
    /// </summary>
    public sealed class MessagePipeObstacleEventSink : IObstacleEventSink
    {
        private readonly IPublisher<ObstacleHitEvent> _hit;

        public MessagePipeObstacleEventSink(IPublisher<ObstacleHitEvent> hit)
        {
            _hit = hit;
        }

        public void Hit(Vector2 position) => _hit.Publish(new ObstacleHitEvent(position));
    }
}
