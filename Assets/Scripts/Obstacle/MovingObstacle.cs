using System;
using DG.Tweening;
using Nestlabs.Level;
using UnityEngine;

namespace Nestlabs.Obstacle
{
    [RequireComponent(typeof(LineRenderer))]
    public class MovingObstacle : ObstacleBase, IPoolable
    {
        [SerializeField] private float duration = 2f;
        [Tooltip("Seconds for one full spin while travelling. 0 disables the spin.")]
        [SerializeField] private float spinDuration = 1.5f;

        [Header("Endpoint markers")]
        [Tooltip("Sprite pinned to travel point 1. Child of this prefab, held at the world endpoint while the body moves/spins.")]
        [SerializeField] private Transform startMarker;
        [Tooltip("Sprite pinned to travel point 2.")]
        [SerializeField] private Transform endMarker;

        private Vector3 startPos;
        private Vector3 endPos;
        private LineRenderer pathLine;
        private bool running;

        private void Awake()
        {
            pathLine = GetComponent<LineRenderer>();
            pathLine.positionCount = 2;
            pathLine.useWorldSpace = true;
        }

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
            transform.rotation = Quaternion.identity;

            // Rope drawn between the two endpoints so the travel path reads before the obstacle
            // reaches you. World-space points, so the spin below doesn't drag the line with it.
            pathLine.positionCount = 2;
            pathLine.SetPosition(0, startPos);
            pathLine.SetPosition(1, endPos);

            running = true;
            PinMarkers();

            transform.DOMove(endPos, duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            if (spinDuration > 0f)
            {
                transform.DORotate(new Vector3(0f, 0f, 360f), spinDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Incremental);
            }
        }

        public void OnDespawned()
        {
            running = false;
            transform.DOKill();
        }

        // Endpoints are fixed world points, but the markers are children of a body that moves and
        // spins - re-pin them in world space every frame so they hold at point 1 / point 2.
        private void LateUpdate()
        {
            if (running) PinMarkers();
        }

        private void PinMarkers()
        {
            if (startMarker != null)
            {
                startMarker.position = startPos;
                startMarker.rotation = Quaternion.identity;
            }
            if (endMarker != null)
            {
                endMarker.position = endPos;
                endMarker.rotation = Quaternion.identity;
            }
        }

        private void OnDestroy()
        {
            transform.DOKill();
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
