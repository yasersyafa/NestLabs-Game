using System;
using MessagePipe;
using NestLabs.Player;
using NestLabs.Shared.Combat;
using NestLabs.Shared.Flow;
using UnityEngine;
using VContainer;

namespace NestLabs
{
    /// <summary>
    /// A single wall of fog that climbs the shaft at a constant speed while a run is active.
    /// Touching it is lethal — it is an <see cref="IDamageSource"/> with damage well above the
    /// player's max health, so the existing hurtbox -> health -> Dead pipeline handles the kill
    /// with no player-side change. PlayerHurtbox never learns this concrete type.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class FogSystem : MonoBehaviour, IDamageSource
    {
        [Header("Tuning")]
        [Tooltip("World units per second the fog line climbs while the run is active.")]
        [SerializeField] private float _riseSpeed = 2f;

        [Tooltip("Lethal in one hit — kept above PlayerConfigSO.MaxHealth so a single contact kills.")]
        [SerializeField] private int _damage = 999;

        [Tooltip("On each run start the fog line snaps to this many units below the player.")]
        [SerializeField] private float _startGapBelowPlayer = 9f;

        private IGameStateService _gameState = NullGameStateService.Instance;
        private PlayerBase _player;
        private IDisposable _subscription;
        private bool _active;

        public int Damage => _damage;

        public Vector2 Position => transform.position;

        /// <summary>
        /// VContainer method injection. Ordering against this component's own Awake is not
        /// guaranteed (same caveat LevelGenerator documents), so this only stores references and
        /// the player transform is read later, from the state handler / Update.
        /// </summary>
        [Inject]
        public void Construct(
            IGameStateService gameState,
            ISubscriber<GameStateChangedEvent> stateChanged,
            PlayerBase player)
        {
            _gameState = gameState ?? NullGameStateService.Instance;
            _player = player;
            _subscription = stateChanged.Subscribe(OnGameStateChanged);
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            switch (e.To)
            {
                // A fresh run (from the ready pose or a restart after death). Resuming from a
                // pause is deliberately excluded so the fog keeps the ground it gained.
                case GameState.Play when e.From != GameState.Pause:
                    ResetBelowPlayer();
                    _active = true;
                    break;

                case GameState.Death:
                case GameState.Menu:
                    _active = false;
                    break;
            }
        }

        private void ResetBelowPlayer()
        {
            if (_player == null) return;

            Vector3 position = transform.position;
            position.y = _player.transform.position.y - _startGapBelowPlayer;
            transform.position = position;
        }

        private void Update()
        {
            // Death and Pause both leave Update running; only advance during an active run.
            if (!_active || !_gameState.IsPlaying) return;

            transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }
}
