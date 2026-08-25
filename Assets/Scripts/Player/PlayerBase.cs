using System;
using NestLabs.Shared.Combat;
using UnityEngine;
using VContainer;

namespace NestLabs.Player
{
    /// <summary>
    /// The player facade. Owns the state machine, builds the single <see cref="PlayerContext"/>
    /// every state reads from, and drives the two tick loops. This is the only class that wires
    /// components together — which is what keeps GetComponent out of every state.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    [DisallowMultipleComponent]
    public sealed class PlayerBase : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerSensor _sensor;
        [SerializeField] private PlayerVisual _visual;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private PlayerHurtbox _hurtbox;

        [Header("Fallbacks")]
        [Tooltip("Used when no container injected a config — lets the prefab run in a bare test scene.")]
        [SerializeField] private PlayerConfigSO _fallbackConfig;

        private PlayerConfigSO _config;
        private IPlayerInput _input;
        private IPlayerEventSink _events;
        private PlayerStateMachine _fsm;
        private PlayerContext _context;
        private bool _ownsInput;

        public PlayerStateMachine Fsm => _fsm;
        public PlayerContext Context => _context;

        /// <summary>
        /// VContainer method injection. Runs during LifetimeScope build, whose ordering against this
        /// component's own Awake is not guaranteed — so this only stores references, and the actual
        /// assembly happens in Start.
        /// </summary>
        [Inject]
        public void Construct(IPlayerInput input, IPlayerEventSink events, PlayerConfigSO config)
        {
            _input = input;
            _events = events;
            _config = config;
        }

        private void Reset()
        {
            _motor = GetComponent<PlayerMotor>();
            _sensor = GetComponent<PlayerSensor>();
            _visual = GetComponent<PlayerVisual>();
            _health = GetComponent<PlayerHealth>();
            _hurtbox = GetComponentInChildren<PlayerHurtbox>();
        }

        private void Start()
        {
            Build();
        }

        private void OnEnable()
        {
            if (_hurtbox != null)
            {
                _hurtbox.DamageDetected += OnDamageDetected;
            }
        }

        private void OnDisable()
        {
            if (_hurtbox != null)
            {
                _hurtbox.DamageDetected -= OnDamageDetected;
            }
        }

        private void OnDestroy()
        {
            if (_ownsInput && _input is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void Update()
        {
            _fsm?.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_fsm == null)
            {
                return;
            }

            _sensor.Probe();
            _fsm.FixedTick(Time.fixedDeltaTime);
        }

        private void Build()
        {
            // Every dependency has a working default so the prefab is playable on its own. A missing
            // container degrades the player to "no events published", never to a null reference.
            if (_config == null)
            {
                _config = _fallbackConfig;
            }

            if (_config == null)
            {
                Debug.LogError($"[PlayerBase] No PlayerConfigSO injected or assigned on '{name}'.", this);
                enabled = false;
                return;
            }

            if (_input == null)
            {
                _input = new TouchPlayerInput();
                _ownsInput = true;
            }

            _events ??= NullPlayerEventSink.Instance;

            _health.Initialize(_config);

            _fsm = new PlayerStateMachine();
            _context = new PlayerContext(
                _fsm, _motor, _sensor, _visual, _health, _config, _input, _events, transform);
            _context.ResetBlackboard();

            _fsm.Register(new LatchState(_context));
            _fsm.Register(new SlideState(_context));
            _fsm.Register(new JumpState(_context));
            _fsm.Register(new FallState(_context));
            _fsm.Register(new DashState(_context));
            _fsm.Register(new HitState(_context));
            _fsm.Register(new DeadState(_context));

            _sensor.Probe();
            _fsm.Initialize(_context, PlayerStateId.Fall);
        }

        private void OnDamageDetected(IDamageSource source)
        {
            if (_fsm == null)
            {
                return;
            }

            // Health owns the i-frame decision; a false result means this contact simply does not count.
            if (!_health.TryApplyDamage(source.Damage, source.Position))
            {
                return;
            }

            // Only accepted hits notify the source, so OnTriggerStay2D re-contacts during i-frames
            // cannot re-fire the obstacle's hit SFX.
            if (source is IHittable hittable)
            {
                hittable.OnHit();
            }

            _events.Hit(source.Damage, _health.Current, _health.GetKnockback());
            _fsm.ChangeState(PlayerStateId.Hit);
        }
    }
}
