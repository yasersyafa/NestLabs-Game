using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// The outward-facing notifications a player raises. States talk to this, never to MessagePipe
    /// directly, so the FSM has no dependency on the messaging library and an EditMode test can run
    /// the whole state machine against <see cref="NullPlayerEventSink"/> with no container at all.
    /// </summary>
    public interface IPlayerEventSink
    {
        void StateChanged(PlayerStateId from, PlayerStateId to);
        void Jumped(int fromWallSide);
        void Dashed(int direction);
        void Latched(int wallSide);
        void Hit(int damage, int remainingHealth, Vector2 knockback);
        void Died(Vector2 position);
    }
}
