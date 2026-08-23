using MessagePipe;
using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Forwards player notifications onto MessagePipe. The single point where the player system
    /// touches the messaging library — swapping bus implementations means replacing this class only.
    /// </summary>
    public sealed class MessagePipePlayerEventSink : IPlayerEventSink
    {
        private readonly IPublisher<PlayerStateChangedEvent> _stateChanged;
        private readonly IPublisher<PlayerJumpedEvent> _jumped;
        private readonly IPublisher<PlayerDashedEvent> _dashed;
        private readonly IPublisher<PlayerLatchedEvent> _latched;
        private readonly IPublisher<PlayerHitEvent> _hit;
        private readonly IPublisher<PlayerDiedEvent> _died;

        public MessagePipePlayerEventSink(
            IPublisher<PlayerStateChangedEvent> stateChanged,
            IPublisher<PlayerJumpedEvent> jumped,
            IPublisher<PlayerDashedEvent> dashed,
            IPublisher<PlayerLatchedEvent> latched,
            IPublisher<PlayerHitEvent> hit,
            IPublisher<PlayerDiedEvent> died)
        {
            _stateChanged = stateChanged;
            _jumped = jumped;
            _dashed = dashed;
            _latched = latched;
            _hit = hit;
            _died = died;
        }

        public void StateChanged(PlayerStateId from, PlayerStateId to) =>
            _stateChanged.Publish(new PlayerStateChangedEvent(from, to));

        public void Jumped(int fromWallSide) =>
            _jumped.Publish(new PlayerJumpedEvent(fromWallSide));

        public void Dashed(int direction) =>
            _dashed.Publish(new PlayerDashedEvent(direction));

        public void Latched(int wallSide) =>
            _latched.Publish(new PlayerLatchedEvent(wallSide));

        public void Hit(int damage, int remainingHealth, Vector2 knockback) =>
            _hit.Publish(new PlayerHitEvent(damage, remainingHealth, knockback));

        public void Died(Vector2 position) =>
            _died.Publish(new PlayerDiedEvent(position));
    }
}
