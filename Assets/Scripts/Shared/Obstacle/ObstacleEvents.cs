using UnityEngine;

namespace NestLabs.Shared.Obstacle
{
    public readonly struct ObstacleHitEvent
    {
        public readonly Vector2 Position;

        public ObstacleHitEvent(Vector2 position)
        {
            Position = position;
        }
    }
}
