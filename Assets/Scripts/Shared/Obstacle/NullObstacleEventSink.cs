using UnityEngine;

namespace NestLabs.Shared.Obstacle
{
    /// <summary>Swallows everything. Default sink for an obstacle spawned outside the container.</summary>
    public sealed class NullObstacleEventSink : IObstacleEventSink
    {
        public static readonly NullObstacleEventSink Instance = new NullObstacleEventSink();

        public void Hit(Vector2 position) { }
    }
}
