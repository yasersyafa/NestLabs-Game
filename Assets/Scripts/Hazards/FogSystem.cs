using System;
using MessagePipe;
using NestLabs.Player;
using NestLabs.Shared.Combat;
using NestLabs.Shared.Flow;
using NestLabs.Shared.Hazards;
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
    public sealed class FogSystem : MonoBehaviour, IDamageSource, IHazardLine
    {
        [Header("Tuning")]
        [Tooltip("World units per second the fog line climbs while the run is active.")]
        [SerializeField] private float _riseSpeed = 2f;

        [Tooltip("Lethal in one hit — kept above PlayerConfigSO.MaxHealth so a single contact kills.")]
        [SerializeField] private int _damage = 999;

        [Tooltip("On each run start the fog line snaps to this many units below the player.")]
        [SerializeField] private float _startGapBelowPlayer = 9f;

        [Tooltip("After a death the line keeps climbing this many real seconds, sealing off the shaft, before it freezes.")]
        [SerializeField] private float _engulfDuration = 1f;

        private IGameStateService _gameState = NullGameStateService.Instance;
        private PlayerBase _player;
        private IDisposable _subscriptions;
        private Collider2D _collider;
        private bool _active;

        // Real-time deadline: a fog death near timeScale 0 (the death slow-mo) must not strand the
        // line mid-rise, so the engulf window is measured in unscaled time.
        private float _engulfUntilRealtime = -1f;

        public int Damage => _damage;

        public Vector2 Position => transform.position;

        public bool IsActive => _active;

        /// <summary>
        /// Top of the lethal volume, which sits several units above <see cref="Position"/> because
        /// the collider is a very tall box offset well below the origin. Read from bounds rather
        /// than the transform so retuning the collider or scaling the prefab stays correct, and so
        /// nobody substitutes <see cref="Position"/> here and culls the level too low.
        /// </summary>
        public float LethalY => _collider != null ? _collider.bounds.max.y : transform.position.y;

        /// <summary>
        /// VContainer method injection. Ordering against this component's own Awake is not
        /// guaranteed (same caveat LevelGenerator documents), so this only stores references and
        /// the player transform is read later, from the state handler / Update.
        /// </summary>
        [Inject]
        public void Construct(
            IGameStateService gameState,
            ISubscriber<GameStateChangedEvent> stateChanged,
            ISubscriber<PlayerDiedEvent> died,
            PlayerBase player)
        {
            _gameState = gameState ?? NullGameStateService.Instance;
            _player = player;

            DisposableBagBuilder bag = DisposableBag.CreateBuilder();
            stateChanged.Subscribe(OnGameStateChanged).AddTo(bag);
            died.Subscribe(OnPlayerDied).AddTo(bag);
            _subscriptions = bag.Build();
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
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
                    _engulfUntilRealtime = -1f;
                    _active = true;
                    break;

                // Death is owned by OnPlayerDied now (a short engulf climb then a freeze), so it
                // must not be handled here too or the two would fight.
                case GameState.Menu:
                    _active = false;
                    _engulfUntilRealtime = -1f;
                    break;
            }
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            // Keep rising a moment longer, then freeze — the black iris covers the screen by the
            // time it stops anyway.
            _engulfUntilRealtime = Time.realtimeSinceStartup + _engulfDuration;
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
            bool engulfing = Time.realtimeSinceStartup < _engulfUntilRealtime;

            // Engulf window elapsed: freeze the line where it ended up, once.
            if (!engulfing && _engulfUntilRealtime > 0f)
            {
                _active = false;
                _engulfUntilRealtime = -1f;
            }

            // Death and Pause both leave Update running. Advance during an active run, or through
            // the post-fog-death engulf window (which climbs even though the run has ended).
            if (!_active && !engulfing) return;
            if (!engulfing && !_gameState.IsPlaying) return;

            transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);
        }

        private void OnDestroy()
        {
            _subscriptions?.Dispose();
        }
    }
}
