using System;
using DG.Tweening;
using Nestlabs.Level;
using UnityEngine;

namespace Nestlabs.Obstacle
{
    public class IdleObstacle : ObstacleBase, IPoolable
    {
        [SerializeField] private float rotationDuration = 2f;
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;

        // Pooled instances never get Start() called again on reactivation, so the pool calls
        // this explicitly every time (fresh or reused) instead.
        public void OnSpawned(Action releaseSelf)
        {
            transform.DORotate(rotationAxis * 360f, rotationDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        public void OnDespawned()
        {
            transform.DOKill();
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
