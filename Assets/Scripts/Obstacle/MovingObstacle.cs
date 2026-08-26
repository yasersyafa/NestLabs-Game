using System;
using DG.Tweening;
using Nestlabs.Level;
using UnityEngine;

namespace Nestlabs.Obstacle
{
    public class MovingObstacle : ObstacleBase, IPoolable
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

        // Pooled instances never get Start() called again on reactivation, so the pool calls
        // this explicitly every time (fresh or reused) instead.
        public void OnSpawned(Action releaseSelf)
        {
            transform.position = startPos;
            tween = transform.DOMove(endPos, duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void OnDespawned()
        {
            tween?.Kill();
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
