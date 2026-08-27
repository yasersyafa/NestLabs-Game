using System;
using DG.Tweening;
using Nestlabs.Level;
using UnityEngine;

namespace Nestlabs.Obstacle
{
    [RequireComponent(typeof(Collider2D))]
    public class ProjectileObstacle : ObstacleBase, IPoolable
    {
        [Header("Timing")]
        [SerializeField] private float moveDuration = 1f;
        [SerializeField] private float warningDuration = 1f;
        [SerializeField] private float warningEdgeMargin = 60f;

        [Header("Facing")]
        [Tooltip("The rest sprite is drawn facing left (travelling -X). Enable if the art faces right instead.")]
        [SerializeField] private bool spriteFacesRight;

        private Collider2D col;
        private SpriteRenderer sprite;
        private Sequence sequence;

        private Vector3 startPos;
        private Vector3 endPos;
        private RectTransform warningUI;
        private Action releaseSelf;

        // Called by a spawner right after Instantiate to drive this instance at runtime.
        public void Configure(Vector3 start, Vector3 end, RectTransform warningIcon)
        {
            startPos = start;
            endPos = end;
            warningUI = warningIcon;
        }

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            // The sprite lives on a child ("Square"), not the root the script sits on.
            sprite = GetComponentInChildren<SpriteRenderer>(true);
        }

        // Pooled instances never get Start() called again on reactivation, so the pool calls
        // this explicitly every time (fresh or reused) instead.
        public void OnSpawned(Action releaseSelf)
        {
            this.releaseSelf = releaseSelf;

            transform.position = startPos;

            // Mirror the sprite so it points along its travel direction. Re-applied every spawn
            // because a pooled instance keeps the previous run's flip.
            if (sprite != null)
            {
                bool movingRight = endPos.x > startPos.x;
                sprite.flipX = movingRight != spriteFacesRight;
            }

            SetProjectileVisible(false);

            sequence = DOTween.Sequence()
                .AppendCallback(ShowWarning)
                .AppendInterval(warningDuration)
                .AppendCallback(Fire)
                .Append(transform.DOMove(endPos, moveDuration).SetEase(Ease.Linear))
                .AppendCallback(() => this.releaseSelf?.Invoke());
        }

        public void OnDespawned()
        {
            sequence?.Kill();
            if (warningUI != null)
            {
                Destroy(warningUI.gameObject);
                warningUI = null;
            }
            SetProjectileVisible(false);
        }

        private void ShowWarning()
        {
            if (warningUI == null) return;
            PositionWarningUI(warningUI);
            warningUI.gameObject.SetActive(true);
        }

        // The camera keeps tracking the player while the warning is up (SimpleCameraFollow runs
        // every LateUpdate), so a one-time position from ShowWarning goes stale within a frame or
        // two. Re-anchor every frame the icon is visible so it stays lined up with spawnY.
        private void Update()
        {
            if (warningUI != null && warningUI.gameObject.activeSelf)
            {
                PositionWarningUI(warningUI);
            }
        }

        private void Fire()
        {
            if (warningUI != null)
            {
                warningUI.gameObject.SetActive(false);
                Destroy(warningUI.gameObject);
            }
            SetProjectileVisible(true);
        }

        // Anchors the warning icon to the screen edge (left/right) at the lane height
        // the projectile will travel through, so it stays put across any screen width
        // instead of being fixed at a world position that may sit off-screen.
        private void PositionWarningUI(RectTransform warning)
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 viewportPoint = cam.WorldToViewportPoint(startPos);
            float anchorX = viewportPoint.x < 0.5f ? 0f : 1f;
            float anchorY = Mathf.Clamp01(viewportPoint.y);

            warning.anchorMin = new Vector2(anchorX, anchorY);
            warning.anchorMax = new Vector2(anchorX, anchorY);
            warning.pivot = new Vector2(anchorX, 0.5f);
            warning.anchoredPosition = new Vector2(anchorX < 0.5f ? warningEdgeMargin : -warningEdgeMargin, 0f);
        }

        private void SetProjectileVisible(bool visible)
        {
            if (sprite != null) sprite.enabled = visible;
            if (col != null) col.enabled = visible;
        }

        private void OnDestroy()
        {
            sequence?.Kill();
            if (warningUI != null) Destroy(warningUI.gameObject);
        }

        public override void OnHit()
        {
            base.OnHit();
            #if UNITY_EDITOR
            Debug.Log("[ProjectileObstacle] on hit function running");
            #endif
        }
    }
}
