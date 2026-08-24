using UnityEngine;

namespace NestLabs.Shared.Obstacle
{
    /// <summary>
    /// The outward-facing notification an obstacle raises. Obstacles talk to this, never to
    /// MessagePipe directly, so Nestlabs.Obstacle never needs to reference the messaging library —
    /// mirrors NestLabs.Player.IPlayerEventSink.
    /// </summary>
    public interface IObstacleEventSink
    {
        void Hit(Vector2 position);
    }
}
