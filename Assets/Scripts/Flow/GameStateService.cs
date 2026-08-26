using System;
using MessagePipe;
using NestLabs.Player;
using NestLabs.Shared.Flow;
using UnityEngine;

namespace NestLabs
{
    /// <summary>
    /// Owns the flow state. Plain class, not a MonoBehaviour: it has no frame work to do, so it
    /// only needs the container's lifetime. Same discipline as AudioEventBinder, the domain code
    /// that raises Death (the player FSM) never learns this type exists.
    /// </summary>
    public sealed class GameStateService : IGameStateService, IDisposable
    {
        private readonly IPublisher<GameStateChangedEvent> _changed;
        private readonly IHitstop _hitstop;
        private readonly IDisposable _subscriptions;

        private GameState _current = GameState.Play;

        public GameState Current => _current;
        public bool IsPlaying => _current == GameState.Play;

        public GameStateService(
            ISubscriber<PlayerDiedEvent> died,
            IPublisher<GameStateChangedEvent> changed,
            IHitstop hitstop)
        {
            _changed = changed;
            _hitstop = NullHitstop.Safe(hitstop);

            DisposableBagBuilder bag = DisposableBag.CreateBuilder();
            // Death is not part of the public API: the run ends because the player died, and the
            // FSM is the only thing that knows that.
            died.Subscribe(_ => Transition(GameState.Death)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void EnterMenu() => Transition(GameState.Menu);
        public void EnterPlay() => Transition(GameState.Play);
        public void Pause() => Transition(GameState.Pause);
        public void Resume() => Transition(GameState.Play);

        public void Dispose() => _subscriptions.Dispose();

        private void Transition(GameState next)
        {
            if (next == _current) return;

            if (!IsAllowed(_current, next))
            {
                Debug.LogWarning($"[GameStateService] Rejected transition {_current} -> {next}.");
                return;
            }

            if (_current == GameState.Pause) _hitstop.SetPaused(false);
            if (next == GameState.Pause) _hitstop.SetPaused(true);

            GameState from = _current;
            // Set before publishing, so a subscriber reading Current inside its handler sees the
            // state it was just told about. PlayerStateMachine.ChangeState orders it the same way.
            _current = next;
            _changed?.Publish(new GameStateChangedEvent(from, next));
        }

        private static bool IsAllowed(GameState from, GameState to) => from switch
        {
            GameState.Menu => to == GameState.Play,
            GameState.Play => to == GameState.Pause || to == GameState.Death,
            GameState.Pause => to == GameState.Play || to == GameState.Menu,
            // Terminal until something explicitly restarts or backs out of the run.
            GameState.Death => to == GameState.Play || to == GameState.Menu,
            _ => false
        };
    }
}
