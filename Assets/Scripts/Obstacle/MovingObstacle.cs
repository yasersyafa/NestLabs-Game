using DG.Tweening;
using UnityEngine;

namespace Nestlabs.Obstacle
{
    public class MovingObstacle : ObstacleBase
    {
        [SerializeField] private float duration = 2f;

        private Vector3 startPos;
        private Vector3 endPos;
        private Tween tween;

        // Called by a spawner right after Instantiate to drive this instance at runtime.
        public void Configure(Vector3 start, Vector3 end)
        {
            startPos = start;
            endPos = end;
        }

        private void Start()
        {
            transform.position = startPos;
            tween = transform.DOMove(endPos, duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDestroy()
        {
            tween?.Kill();
        }

        public override void OnHit()
        {
            base.OnHit();
            #if UNITY_EDITOR
            Debug.Log("[MovingObstacle] on hit function running");
            #endif
        }
    }
}
