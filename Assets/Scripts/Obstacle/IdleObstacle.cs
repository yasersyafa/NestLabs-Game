using DG.Tweening;
using UnityEngine;

namespace Nestlabs.Obstacle
{
    public class IdleObstacle : ObstacleBase
    {
        [SerializeField] private float rotationDuration = 2f;
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;

        private void Start()
        {
            transform.DORotate(rotationAxis * 360f, rotationDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }

        public override void OnHit()
        {
            base.OnHit();
            #if UNITY_EDITOR
            Debug.Log("[IdleObstacle] on hit function running");
            #endif
        }
    }
}
