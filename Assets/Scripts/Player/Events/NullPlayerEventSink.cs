using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>Swallows everything. Default sink when no container has injected a real one.</summary>
    public sealed class NullPlayerEventSink : IPlayerEventSink
    {
        public static readonly NullPlayerEventSink Instance = new NullPlayerEventSink();

        public void StateChanged(PlayerStateId from, PlayerStateId to) { }
        public void Jumped(int fromWallSide) { }
        public void Dashed(int direction) { }
        public void Latched(int wallSide) { }
        public void Hit(int damage, int remainingHealth, Vector2 knockback) { }
        public void Died(Vector2 position) { }
    }
}
