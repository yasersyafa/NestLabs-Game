using UnityEngine;

namespace NestLabs
{
    /// <summary>
    /// The real time-scale dip. Lives on a services object so it outlives any one player: a player
    /// destroyed mid-effect cannot leave the game running slow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Hitstop : MonoBehaviour, IHitstop
    {
        private float _remaining;
        private float _scale = 1f;
        private bool _paused;

        // Time.timeScale survives scene loads and exiting play mode, so a session that ended
        // mid-dip would otherwise start the next one already slowed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }

        public void Begin(float scale, float unscaledDuration)
        {
            if (unscaledDuration <= 0f || scale >= 1f)
            {
                Cancel();
                return;
            }

            _scale = Mathf.Clamp(scale, 0f, 1f);
            _remaining = unscaledDuration;
            // A pause outranks the dip. Record it anyway so unpausing resumes what was running.
            if (!_paused) Time.timeScale = _scale;
        }

        public void Cancel()
        {
            _remaining = 0f;
            _scale = 1f;
            if (!_paused) Time.timeScale = 1f;
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused) return;

            _paused = paused;
            Time.timeScale = paused ? 0f : (_remaining > 0f ? _scale : 1f);
        }

        private void Update()
        {
            // Unscaled delta keeps running at timeScale 0, so without this a pause would burn
            // through any dip that was live when it started.
            if (_paused || _remaining <= 0f) return;

            // Unscaled, or the dip would stretch itself by its own factor.
            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f) Cancel();
        }

        private void OnDisable()
        {
            // Scene reloads and teardown must never leave the game slowed or frozen.
            bool wasHolding = _paused || _remaining > 0f;
            _paused = false;
            _remaining = 0f;
            _scale = 1f;
            if (wasHolding) Time.timeScale = 1f;
        }
    }
}
