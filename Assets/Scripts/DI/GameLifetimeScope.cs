using MessagePipe;
using NestLabs.Player;
using NestLabs.Score;
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

            builder.Register<IPlayerEventSink, MessagePipePlayerEventSink>(Lifetime.Singleton);

            // TouchPlayerInput is IDisposable; the container tears its InputAction down with the scope.
            builder.Register<IPlayerInput, TouchPlayerInput>(Lifetime.Singleton);

            builder.RegisterInstance(_playerConfig);

            builder.RegisterComponentInHierarchy<PlayerBase>();
            builder.RegisterComponentInHierarchy<PlayerDebugHud>();
            builder.RegisterComponentInHierarchy<ScoreService>();
        }
    }
}
