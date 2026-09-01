using UnityEngine;

namespace NestLabs
{
    /// <summary>
    /// Keeps the climber in frame. Vertical-only by default, because a wall-to-wall shaft has a
    /// fixed width — following X as well would make the walls appear to drift.
    /// Also serves the death choreography's <see cref="ICameraShake"/>: an additive Perlin offset
    /// layered on top of the follow position, subtracted and re-applied each frame so it never
    /// feeds back into the SmoothDamp.
    /// </summary>
    public sealed class SimpleCameraFollow : MonoBehaviour, ICameraShake
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector2 _offset = new Vector2(0f, 1.5f);
        [SerializeField] private float _smoothTime = 0.18f;
        [SerializeField] private bool _followHorizontally;

        private Vector3 _velocity;

        // Shake state. Real time on purpose — a fog death runs the shake through a hitstop dip.
        private float _shakeAmplitude;
        private float _shakeDuration;
        private float _shakeFrequency;
        private float _shakeElapsed;
        private Vector2 _shakeSeed;
        private Vector3 _appliedShake;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void Shake(float amplitude, float duration, float frequency)
        {
            if (amplitude <= 0f || duration <= 0f)
            {
                return;
            }

            _shakeAmplitude = amplitude;
            _shakeDuration = duration;
            _shakeFrequency = Mathf.Max(0.01f, frequency);
            _shakeElapsed = 0f;
            // Fresh noise lanes each call so a retrigger does not resample the same wobble.
            _shakeSeed = new Vector2(Random.value * 100f, Random.value * 100f);
        }

        private void LateUpdate()
        {
            // Undo last frame's shake before reading position, so SmoothDamp only ever sees the
            // clean follow position and the shake cannot compound.
            transform.position -= _appliedShake;

            if (_target != null)
            {
                Vector3 current = transform.position;
                var goal = new Vector3(
                    _followHorizontally ? _target.position.x + _offset.x : current.x,
                    _target.position.y + _offset.y,
                    current.z);

                transform.position = Vector3.SmoothDamp(current, goal, ref _velocity, _smoothTime);
            }

            _appliedShake = Vector3.zero;

            if (_shakeElapsed < _shakeDuration)
            {
                _shakeElapsed += Time.unscaledDeltaTime;

                // Quadratic falloff: hard hit, quick settle.
                float k = 1f - Mathf.Clamp01(_shakeElapsed / _shakeDuration);
                float falloff = k * k;

                float phase = _shakeElapsed * _shakeFrequency;
                float ox = (Mathf.PerlinNoise(_shakeSeed.x, phase) - 0.5f) * 2f;
                float oy = (Mathf.PerlinNoise(_shakeSeed.y, phase) - 0.5f) * 2f;

                _appliedShake = new Vector3(ox, oy, 0f) * (_shakeAmplitude * falloff);
            }

            transform.position += _appliedShake;
        }
    }
}
