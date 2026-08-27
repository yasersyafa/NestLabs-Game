using System;
using MessagePipe;
using NestLabs.Player;
using NestLabs.Score;
using NestLabs.Shared.Flow;
using NestLabs.Shared.Obstacle;

namespace NestLabs.Audio
{
    /// <summary>
    /// Translates domain events into SFX calls. AudioService itself never sees MessagePipe or
    /// knows which events exist — same discipline as IPlayerEventSink keeping the player FSM
    /// ignorant of the messaging library. Adding a new SFX trigger later only touches this class.
    /// </summary>
    public sealed class AudioEventBinder : IDisposable
    {
        private readonly IDisposable _subscriptions;

        public AudioEventBinder(
            IAudioService audio,
            ISubscriber<GameStateChangedEvent> gameStateChanged,
            ISubscriber<PlayerJumpedEvent> jumped,
            ISubscriber<PlayerDashedEvent> dashed,
            ISubscriber<PlayerLatchedEvent> latched,
            ISubscriber<PlayerHitEvent> hit,
            ISubscriber<PlayerDiedEvent> died,
            ISubscriber<ScoreFinalizedEvent> scoreFinalized,
            ISubscriber<ObstacleHitEvent> obstacleHit)
        {
            DisposableBagBuilder bag = DisposableBag.CreateBuilder();

            // Music has no dedicated binder like SFX — the flow state drives it. Gameplay music
            // starts when a run begins and stops back at the menu or on death. Resuming from Pause
            // is not a fresh start, so it is excluded (PlayMusic would no-op on the same id anyway).
            gameStateChanged.Subscribe(e =>
            {
                switch (e.To)
                {
                    case GameState.Play when e.From != GameState.Pause:
                        audio.PlayMusic(MusicId.Gameplay);
                        break;
                    case GameState.Menu:
                    case GameState.Death:
                        audio.StopMusic();
                        break;
                }
            }).AddTo(bag);

            jumped.Subscribe(_ => audio.PlaySfx(SfxId.Jump)).AddTo(bag);
            dashed.Subscribe(_ => audio.PlaySfx(SfxId.Dash)).AddTo(bag);
            latched.Subscribe(_ => audio.PlaySfx(SfxId.Latch)).AddTo(bag);
            hit.Subscribe(_ => audio.PlaySfx(SfxId.Hit)).AddTo(bag);
            died.Subscribe(_ => audio.PlaySfx(SfxId.Death)).AddTo(bag);
            // ScoreChangedEvent fires every frame the player climbs, so it is deliberately NOT
            // wired here — that would spam a SFX at up to 60/sec. ScoreFinalizedEvent fires once
            // per run instead.
            scoreFinalized.Subscribe(_ => audio.PlaySfx(SfxId.ScoreFinalize)).AddTo(bag);
            obstacleHit.Subscribe(_ => audio.PlaySfx(SfxId.ObstacleHit)).AddTo(bag);

            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions.Dispose();
    }
}
