using System;
using System.Collections.Generic;
using MessagePipe;
using NestLabs.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer;

namespace NestLabs
{
    /// <summary>
    /// On-screen readout of the player's live state plus a rolling feed of published events.
    /// Doubles as proof that the VContainer + MessagePipe chain is actually wired: if the feed
    /// stays empty while the state readout moves, the container never injected this component.
    /// </summary>
    public sealed class PlayerDebugHud : MonoBehaviour
    {
        private const int MaxLines = 12;

        [SerializeField] private PlayerBase _player;

        private readonly List<string> _feed = new List<string>();
        private IDisposable _subscriptions;
        private GUIStyle _style;

        [Inject]
        public void Construct(
            ISubscriber<PlayerStateChangedEvent> stateChanged,
            ISubscriber<PlayerJumpedEvent> jumped,
            ISubscriber<PlayerDashedEvent> dashed,
            ISubscriber<PlayerLatchedEvent> latched,
            ISubscriber<PlayerHitEvent> hit,
            ISubscriber<PlayerDiedEvent> died)
        {
            DisposableBagBuilder bag = DisposableBag.CreateBuilder();

            stateChanged.Subscribe(e => Append($"{e.From} -> {e.To}")).AddTo(bag);
            jumped.Subscribe(e => Append($"Jumped off wall {e.FromWallSide}")).AddTo(bag);
            dashed.Subscribe(e => Append($"Dashed dir {e.Direction}")).AddTo(bag);
            latched.Subscribe(e => Append($"Latched wall {e.WallSide}")).AddTo(bag);
            hit.Subscribe(e => Append($"Hit -{e.Damage}, hp now {e.RemainingHealth}")).AddTo(bag);
            died.Subscribe(_ => Append("Died")).AddTo(bag);

            _subscriptions = bag.Build();
        }

        private void Awake()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerBase>();
            }
        }

        private void OnDestroy()
        {
            _subscriptions?.Dispose();
        }

        private void Update()
        {
            // Dead is terminal by design, so testing needs a way back without leaving play mode.
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void Append(string line)
        {
            _feed.Add($"{Time.time,6:0.00}  {line}");
            if (_feed.Count > MaxLines)
            {
                _feed.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 16, richText = false };

            string header = "Player not built yet";
            if (_player != null && _player.Context != null)
            {
                PlayerContext ctx = _player.Context;
                header =
                    $"State: {_player.Fsm.CurrentId}    " +
                    $"HP: {ctx.Health.Current}    " +
                    $"Dash: {ctx.DashChargesRemaining}    " +
                    $"Wall: {ctx.Sense.WallSide}    " +
                    $"Vel: {ctx.Motor.Velocity.x:0.0}, {ctx.Motor.Velocity.y:0.0}";
            }

            GUI.Label(new Rect(12f, 8f, 900f, 26f), header, _style);
            GUI.Label(new Rect(12f, 34f, 900f, 26f),
                "Tap / left-click to act    |    R to restart", _style);

            if (_subscriptions == null)
            {
                GUI.Label(new Rect(12f, 60f, 900f, 26f),
                    "NO CONTAINER: events not subscribed (GameLifetimeScope missing?)", _style);
                return;
            }

            for (int i = 0; i < _feed.Count; i++)
            {
                GUI.Label(new Rect(12f, 66f + i * 20f, 900f, 22f), _feed[i], _style);
            }
        }
    }
}
