using DG.Tweening;
using UnityEngine;

namespace Nestlabs.Obstacle
{
    // Pendulum obstacle: hangs from a fixed anchor point and arcs back and forth between
    // -maxAngle and +maxAngle. Position is driven directly (angle -> world offset from the
    // anchor) rather than through a parent pivot transform, so it stays a single-GameObject
    // prefab like IdleObstacle/MovingObstacle instead of needing a pivot+child hierarchy.
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SwingObstacle : ObstacleBase
    {
        [Header("Swing")]
        [Tooltip("Distance from the anchor to the obstacle, in world units.")]
        [SerializeField] private float ropeLength = 3f;
        [Tooltip("Degrees from straight down, each side. Total arc width is double this.")]
        [SerializeField] private float maxAngle = 60f;
        [Tooltip("Seconds for one full there-and-back swing.")]
        [SerializeField] private float swingPeriod = 2f;
        [SerializeField] private Ease swingEase = Ease.InOutSine;

        // Exposed so the spawner can read the prefab's own tuning (rope length, max angle) to
        // keep the anchor clear of the screen edge, instead of duplicating these numbers on the
        // spawner too.
        public float RopeLength => ropeLength;
        public float MaxAngle => maxAngle;

        private LineRenderer _rope;
        private Vector3 _anchorPos;
        private float _currentAngle;
        private Tweener _swingTween;

        // Called by the spawner right after Instantiate.
        public void Configure(Vector3 anchor)
        {
            _anchorPos = anchor;
        }

        // RequireComponent guarantees LineRenderer/CircleCollider2D/SpriteRenderer already exist
        // once this is added in the Editor — this just sets sane defaults on them so a fresh
        // SwingObstacle is visible and hittable without extra manual setup.
        private void Reset()
        {
            var line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.numCapVertices = 4;
            if (line.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null) line.sharedMaterial = new Material(shader);
            }

            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
        }

        private void Awake()
        {
            _rope = GetComponent<LineRenderer>();
            _rope.positionCount = 2;
        }

        private void Start()
        {
            _currentAngle = Random.value < 0.5f ? -maxAngle : maxAngle;
            UpdatePosition();

            float startAngle = _currentAngle;
            float endAngle = -_currentAngle;

            _swingTween = DOTween.To(() => _currentAngle, a =>
                {
                    _currentAngle = a;
                    UpdatePosition();
                }, endAngle, swingPeriod * 0.5f)
                .From(startAngle)
                .SetEase(swingEase)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void UpdatePosition()
        {
            float rad = _currentAngle * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Sin(rad), -Mathf.Cos(rad), 0f) * ropeLength;
            transform.position = _anchorPos + offset;

            _rope.SetPosition(0, _anchorPos);
            _rope.SetPosition(1, transform.position);
        }

        private void OnDestroy()
        {
            _swingTween?.Kill();
        }

        public override void OnHit()
        {
            base.OnHit();
            #if UNITY_EDITOR
            Debug.Log("[SwingObstacle] on hit function running");
            #endif
        }
    }
}
