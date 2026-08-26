using MessagePipe;
using Nestlabs.Level;
using NestLabs.Audio;
using NestLabs.Player;
using NestLabs.Score;
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

            builder.Register<IPlayerEventSink, MessagePipePlayerEventSink>(Lifetime.Singleton);
            builder.Register<IObstacleEventSink, MessagePipeObstacleEventSink>(Lifetime.Singleton);

            // TouchPlayerInput is IDisposable; the container tears its InputAction down with the scope.
            builder.Register<IPlayerInput, TouchPlayerInput>(Lifetime.Singleton);

            builder.RegisterInstance(_playerConfig);
            builder.RegisterInstance(_audioLibrary);
            builder.Register<IAudioMuteStore, PlayerPrefsAudioMuteStore>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<PlayerBase>();
            builder.RegisterComponentInHierarchy<PlayerDebugHud>();
            builder.RegisterComponentInHierarchy<ScoreService>();
            builder.RegisterComponentInHierarchy<AudioService>().As<IAudioService>();
            builder.RegisterComponentInHierarchy<LevelGenerator>();

            // AudioEventBinder has no MonoBehaviour and nothing else resolves it — force-resolve
            // it once at container build so its constructor (and its MessagePipe subscriptions)
            // actually runs. Skipping this leaves audio silent with no error.
            builder.Register<AudioEventBinder>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver => resolver.Resolve<AudioEventBinder>());
        }
    }
}
