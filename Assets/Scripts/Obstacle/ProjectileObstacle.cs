using DG.Tweening;
using UnityEngine;

namespace Nestlabs.Obstacle
{
    [RequireComponent(typeof(Collider2D))]
    public class ProjectileObstacle : ObstacleBase
    {
        [Header("Timing")]
        [SerializeField] private float moveDuration = 1f;
        [SerializeField] private float warningDuration = 1f;
        [SerializeField] private float warningEdgeMargin = 60f;

        private Collider2D col;
        private SpriteRenderer sprite;
        private Sequence sequence;

        private Vector3 startPos;
        private Vector3 endPos;
        private RectTransform warningUI;

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
            sprite = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            transform.position = startPos;
            SetProjectileVisible(false);

            sequence = DOTween.Sequence()
                .AppendCallback(ShowWarning)
                .AppendInterval(warningDuration)
                .AppendCallback(Fire)
                .Append(transform.DOMove(endPos, moveDuration).SetEase(Ease.Linear))
                .AppendCallback(() => Destroy(gameObject));
        }

        private void ShowWarning()
        {
            if (warningUI == null) return;
            PositionWarningUI(warningUI);
            warningUI.gameObject.SetActive(true);
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
            #if UNITY_EDITOR
            Debug.Log("[ProjectileObstacle] on hit function running");
            #endif
        }
    }
}
