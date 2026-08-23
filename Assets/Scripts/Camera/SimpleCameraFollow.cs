using UnityEngine;

namespace NestLabs
{
    /// <summary>
    /// Keeps the climber in frame. Vertical-only by default, because a wall-to-wall shaft has a
    /// fixed width — following X as well would make the walls appear to drift.
    /// </summary>
    public sealed class SimpleCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector2 _offset = new Vector2(0f, 1.5f);
        [SerializeField] private float _smoothTime = 0.18f;
        [SerializeField] private bool _followHorizontally;

        private Vector3 _velocity;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 current = transform.position;
            var goal = new Vector3(
                _followHorizontally ? _target.position.x + _offset.x : current.x,
                _target.position.y + _offset.y,
                current.z);

            transform.position = Vector3.SmoothDamp(current, goal, ref _velocity, _smoothTime);
        }
    }
}
