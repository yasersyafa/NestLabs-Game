using MessagePipe;
using Nestlabs.Level;
using NestLabs.Audio;
using NestLabs.Player;
using NestLabs.Score;
using NestLabs.Shared.Flow;
using NestLabs.UI;
using NestLabs.Shared.Obstacle;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NestLabs
{
    /// <summary>
    /// Composition root. Everything the player needs from outside itself is registered here, which
    /// is what lets PlayerBase take its dependencies through Construct instead of reaching for
    /// singletons.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private PlayerConfigSO _playerConfig;
        [SerializeField] private AudioLibrarySO _audioLibrary;

        protected override void Configure(IContainerBuilder builder)
        {
            MessagePipeOptions options = builder.RegisterMessagePipe();

            builder.RegisterMessageBroker<PlayerStateChangedEvent>(options);
            builder.RegisterMessageBroker<PlayerJumpedEvent>(options);
            builder.RegisterMessageBroker<PlayerDashedEvent>(options);
            builder.RegisterMessageBroker<PlayerLatchedEvent>(options);
            builder.RegisterMessageBroker<PlayerHitEvent>(options);
            builder.RegisterMessageBroker<PlayerDiedEvent>(options);
            builder.RegisterMessageBroker<ScoreChangedEvent>(options);
            builder.RegisterMessageBroker<ScoreFinalizedEvent>(options);
            builder.RegisterMessageBroker<ObstacleHitEvent>(options);
            builder.RegisterMessageBroker<GameStateChangedEvent>(options);

            builder.Register<IPlayerEventSink, MessagePipePlayerEventSink>(Lifetime.Singleton);
            builder.Register<IObstacleEventSink, MessagePipeObstacleEventSink>(Lifetime.Singleton);

            // TouchPlayerInput is IDisposable; the container tears its InputAction down with the scope.
            builder.Register<IPlayerInput, TouchPlayerInput>(Lifetime.Singleton);

            // RegisterInstance throws on a null instance, which aborts the whole Configure and
            // leaves every other component uninjected. The downstream symptom is a null resolver
            // somewhere unrelated, so fail loudly here naming the field that is actually missing.
            if (!RegisterRequired(builder, _playerConfig, nameof(_playerConfig))) return;
            if (!RegisterRequired(builder, _audioLibrary, nameof(_audioLibrary))) return;
            builder.Register<IAudioMuteStore, PlayerPrefsAudioMuteStore>(Lifetime.Singleton);
            builder.Register<IScoreStore, PlayerPrefsScoreStore>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<PlayerBase>();
            builder.RegisterComponentInHierarchy<PlayerDebugHud>();
            builder.RegisterComponentInHierarchy<ScoreService>();
            builder.RegisterComponentInHierarchy<ScoreHud>();
            // Only dev-awe carries the HUD prefab; YaserScene and the other rigs are gameplay-only.
            // RegisterComponentInHierarchy throws when its target is missing, so registering
            // unconditionally would break the container in every scene without the overlay.
            if (ExistsInScene<HudPanelController>())
            {
                builder.RegisterComponentInHierarchy<HudPanelController>();
            }
            builder.RegisterComponentInHierarchy<AudioService>().As<IAudioService>();
            builder.RegisterComponentInHierarchy<LevelGenerator>();
            builder.RegisterComponentInHierarchy<FogSystem>();
            builder.RegisterComponentInHierarchy<Hitstop>().As<IHitstop>();

            builder.Register<IGameStateService, GameStateService>(Lifetime.Singleton);

            // Neither of these is a MonoBehaviour and nothing else resolves them yet —
            // force-resolve once at container build so their constructors (and their MessagePipe
            // subscriptions) actually run. Skipping this leaves audio silent and the game state
            // stuck in Play after a death, with no error either way.
            builder.Register<AudioEventBinder>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<AudioEventBinder>();
                resolver.Resolve<IGameStateService>();
            });
        }

        /// <summary>
        /// Mirrors how VContainer resolves <c>RegisterComponentInHierarchy</c>: same scene, and
        /// inactive objects included. Used to skip a registration whose target this scene lacks.
        /// </summary>
        private bool ExistsInScene<T>() where T : Component
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<T>(true) != null) return true;
            }

            return false;
        }

        private bool RegisterRequired<T>(IContainerBuilder builder, T asset, string field) where T : class
        {
            if (asset == null)
            {
                Debug.LogError(
                    $"[GameLifetimeScope] '{field}' is not assigned on '{name}'. No dependency will " +
                    "be injected until it is.", this);
                return false;
            }

            builder.RegisterInstance(asset);
            return true;
        }
    }
}
