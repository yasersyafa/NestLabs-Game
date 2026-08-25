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

            Time.timeScale = Mathf.Clamp(scale, 0f, 1f);
            _remaining = unscaledDuration;
        }

        public void Cancel()
        {
            _remaining = 0f;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (_remaining <= 0f) return;

            // Unscaled, or the dip would stretch itself by its own factor.
            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f) Cancel();
        }

        private void OnDisable()
        {
            // Scene reloads and teardown must never leave the game slowed.
            if (_remaining > 0f) Cancel();
        }
    }
}
