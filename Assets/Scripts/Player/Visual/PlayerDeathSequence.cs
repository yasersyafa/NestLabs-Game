using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NestLabs.UI;
using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Drives the death choreography. Lives on the player root rather than inside a state because
    /// <see cref="DeadState"/> is terminal and never ticked — the sequence has to run itself. The
    /// player vanishes instantly, a shard burst plus a camera kick sell the moment, then a black
    /// disc blooms out from the death point until it covers the screen. When it finishes it raises
    /// <c>PlayerDeathSequenceCompletedEvent</c>, which is what lets the game-over panel appear.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDeathSequence : MonoBehaviour
    {
        private PlayerContext _ctx;
        private PlayerDeathBurst _burst;
        private ICameraShake _cameraShake = NullCameraShake.Instance;
        private IScreenTransition _screenTransition = NullScreenTransition.Instance;
        private CancellationTokenSource _cts;

        /// <summary>Called once by <see cref="PlayerBase.Build"/> after the context is assembled.</summary>
        public void Initialize(
            PlayerContext context,
            PlayerDeathBurst burst,
            ICameraShake cameraShake,
            IScreenTransition screenTransition)
        {
            _ctx = context;
            _burst = burst;
            _cameraShake = cameraShake ?? NullCameraShake.Instance;
            _screenTransition = screenTransition ?? NullScreenTransition.Instance;
        }

        /// <summary>Starts (or restarts) the death sequence.</summary>
        public void Play()
        {
            if (_ctx == null)
            {
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            RunAsync(_cts.Token).Forget();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            PlayerConfigSO cfg = _ctx.Config;

            try
            {
                _ctx.Visual.BeginDeath();

                // The player is gone the instant it dies (Visual.BeginDeath switched the sprite
                // off). The burst, the camera kick and the black bloom carry the moment.
                Vector3 pos = _ctx.Transform.position;

                _burst?.Play(pos);
                _cameraShake.Shake(
                    cfg.DeathShakeAmplitude, cfg.DeathShakeDuration, cfg.DeathShakeFrequency);

                if (cfg.DeathSlowScale < 1f)
                {
                    _ctx.Hitstop.Begin(cfg.DeathSlowScale, cfg.DeathSlowDuration);
                }

                // Let the burst read on its own for a beat. Real time so the hitstop dip does not
                // stretch it.
                await UniTask.Delay(
                    TimeSpan.FromSeconds(cfg.DeathBurstLead), ignoreTimeScale: true, cancellationToken: ct);

                // A black disc blooms out from the death point until it covers the screen.
                await _screenTransition.CloseAsync(pos, cfg.DeathIrisCloseDuration, ct);

                // Real time so a lingering hitstop dip cannot stretch the final beat.
                await UniTask.Delay(
                    TimeSpan.FromSeconds(cfg.DeathHoldDuration), ignoreTimeScale: true, cancellationToken: ct);

                _ctx.Events.DeathSequenceCompleted();
            }
            catch (OperationCanceledException)
            {
                // Scene reload or a second death — nothing to unwind, the tweens are SetLink'd.
            }
        }
    }
}
