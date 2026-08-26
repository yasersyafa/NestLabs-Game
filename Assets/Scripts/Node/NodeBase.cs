using System;
using DG.Tweening;
using Nestlabs.Level;
using UnityEngine;

namespace NestLabs.Node
{
    /// <summary>
    /// A grapple point. Holds no player knowledge: it publishes a position and a launch force, and
    /// the player's node sensor finds it through the trigger on the Radius child. All tuning comes
    /// from <see cref="NodeDataSO"/> so variants differ by asset, not by script.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NodeBase : MonoBehaviour, IPoolable
    {
        [Header("Data")]
        [SerializeField] private NodeDataSO _data;

        [Header("Parts")]
        [SerializeField] private CircleCollider2D _radius;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Transform _vfxRoot;

        [Tooltip("Draws the range in the Game view and in a build. Sits under the Radius child so it inherits the collider's transform exactly.")]
        [SerializeField] private LineRenderer _rangeRing;

        [Header("Debug")]
        [Tooltip("Draw the launch range in the Scene view.")]
        [SerializeField] private bool _drawGizmos = true;

        private float _readyAt;
        private Tween _popTween;
        private Tween _flashTween;
        private Vector3 _spriteBaseScale = Vector3.one;

        public Vector2 Position => transform.position;

        public float LaunchForce => _data != null ? _data.LaunchForce : 0f;

        /// <summary>Grab radius, read off the serialized data so it is valid on a prefab asset too.</summary>
        public float ClaimRadius => _data != null ? _data.Radius : 0f;

        public bool IsReady => _data == null || Time.time >= _readyAt;

        /// <summary>Set by EditMode tests, which stage a node without going through the asset pipeline.</summary>
        internal NodeDataSO Data
        {
            get => _data;
            set
            {
                _data = value;
                ApplyData();
            }
        }

        private void Reset()
        {
            FindParts();
            ApplyData();
        }

        private void OnValidate()
        {
            // Keeps the collider the designer sees in the Scene view honest about the asset's radius.
            if (_radius == null || _sprite == null || _rangeRing == null || _vfxRoot == null) FindParts();
            ApplyData();
        }

        private void Awake()
        {
            if (_radius == null || _sprite == null || _rangeRing == null || _vfxRoot == null) FindParts();

            if (_data == null)
            {
                Debug.LogError($"[NodeBase] No NodeDataSO assigned on '{name}'.", this);
                enabled = false;
                return;
            }

            if (_sprite != null) _spriteBaseScale = _sprite.transform.localScale;

            ApplyData();
        }

        private void OnDestroy()
        {
            _popTween?.Kill();
            _flashTween?.Kill();
        }

        // Nothing to (re)start on spawn - Awake's structural wiring (FindParts/ApplyData)
        // doesn't need to redo per reuse, and cooldown/tint reset happens on despawn instead.
        public void OnSpawned(Action releaseSelf)
        {
        }

        // Pooled reuse skips OnDestroy, so a Node returning to the pool mid-cooldown or
        // mid-pop-tween must reset both here or it could come back still looking/acting spent.
        public void OnDespawned()
        {
            _popTween?.Kill();
            _flashTween?.Kill();
            _readyAt = 0f;

            if (_data != null)
            {
                if (_sprite != null) _sprite.color = _data.Tint;
                SetRingColor(_data.Tint);
            }
        }

        private void Update()
        {
            // Only runs while spent. A node with no cooldown costs one comparison a frame.
            if (_readyAt <= 0f || Time.time < _readyAt) return;

            _readyAt = 0f;
            if (_sprite != null) _sprite.color = _data.Tint;
            SetRingColor(_data.Tint);
        }

        /// <summary>Called by the player the instant a launch starts. Puts the node on cooldown.</summary>
        public void Consume()
        {
            if (_data == null) return;

            if (_data.ReuseCooldown > 0f)
            {
                _readyAt = Time.time + _data.ReuseCooldown;
            }

            PlayPop();

            // Allocates per launch. Pool this if nodes ever get dense.
            if (_data.LaunchVfx != null)
            {
                Transform parent = _vfxRoot != null ? _vfxRoot : transform;
                Instantiate(_data.LaunchVfx, parent.position, Quaternion.identity, parent);
            }
        }

        private void FindParts()
        {
            if (_radius == null) _radius = GetComponentInChildren<CircleCollider2D>();
            if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_rangeRing == null) _rangeRing = GetComponentInChildren<LineRenderer>();
            if (_vfxRoot == null) _vfxRoot = transform.Find("Vfx");
            if (_vfxRoot == null) _vfxRoot = transform;
        }

        private void ApplyData()
        {
            if (_data == null) return;

            if (_radius != null)
            {
                _radius.isTrigger = true;
                _radius.radius = _data.Radius;
            }

            if (_sprite != null) _sprite.color = _data.Tint;

            BuildRangeRing();
        }

        /// <summary>
        /// Rebuilds the in-game range ring. Points are local and the ring parents under the Radius
        /// child, so it tracks the collider's own position and scale with no per-frame work.
        /// </summary>
        private void BuildRangeRing()
        {
            if (_rangeRing == null) return;

            if (_data == null || !_data.ShowRange)
            {
                _rangeRing.enabled = false;
                return;
            }

            _rangeRing.enabled = true;
            _rangeRing.useWorldSpace = false;
            _rangeRing.loop = true;
            _rangeRing.widthMultiplier = _data.RangeRingWidth;

            int segments = Mathf.Clamp(_data.RangeRingSegments, 12, 128);
            if (_rangeRing.positionCount != segments) _rangeRing.positionCount = segments;

            float step = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * step;
                _rangeRing.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * _data.Radius);
            }

            SetRingColor(IsReady ? _data.Tint : _data.SpentTint);
        }

        /// <summary>
        /// Grab reaction: a scale punch plus a flash that settles into whichever tint the node is
        /// about to sit at. Runs unscaled because the grab happens during the player's slow-mo.
        /// </summary>
        private void PlayPop()
        {
            Color settle = _readyAt > 0f ? _data.SpentTint : _data.Tint;

            if (_sprite != null && _data.PopScale > 0f && _data.PopDuration > 0f)
            {
                _popTween?.Kill();
                _sprite.transform.localScale = _spriteBaseScale;
                _popTween = _sprite.transform
                    .DOPunchScale(_spriteBaseScale * _data.PopScale, _data.PopDuration, vibrato: 1, elasticity: 0.4f)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }

            if (_data.PopDuration <= 0f)
            {
                if (_sprite != null) _sprite.color = settle;
                SetRingColor(settle);
                return;
            }

            _flashTween?.Kill();
            if (_sprite != null) _sprite.color = _data.PopFlash;
            SetRingColor(_data.PopFlash);

            // One driver for both renderers so they never drift apart.
            float t = 0f;
            _flashTween = DOTween.To(() => t, v =>
                {
                    t = v;
                    Color c = Color.Lerp(_data.PopFlash, settle, v);
                    if (_sprite != null) _sprite.color = c;
                    SetRingColor(c);
                }, 1f, _data.PopDuration)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void SetRingColor(Color color)
        {
            if (_rangeRing == null) return;

            _rangeRing.startColor = color;
            _rangeRing.endColor = color;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawRange(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawRange(true);
        }

        /// <summary>
        /// Draws the launch range. The radius is read off the collider rather than the asset, so a
        /// node whose data never applied shows the range that is actually live, not the intended one.
        /// </summary>
        private void DrawRange(bool selected)
        {
            if (!_drawGizmos || _radius == null) return;

            Transform t = _radius.transform;
            Vector3 center = t.TransformPoint(_radius.offset);
            Vector3 scale = t.lossyScale;
            float radius = _radius.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));

            Color tint = _data != null ? _data.Tint : Color.white;
            if (!IsReady) tint = Color.red;

            UnityEditor.Handles.color = new Color(tint.r, tint.g, tint.b, selected ? 0.12f : 0.05f);
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.forward, radius);

            UnityEditor.Handles.color = new Color(tint.r, tint.g, tint.b, selected ? 1f : 0.5f);
            UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, radius, selected ? 2f : 1f);

            // The pull aims at the node's own position, which is not the collider's centre when the
            // Radius child is offset. Seeing both apart is the point of drawing them separately.
            Vector3 target = transform.position;
            const float Tick = 0.18f;
            UnityEditor.Handles.DrawLine(target + Vector3.left * Tick, target + Vector3.right * Tick);
            UnityEditor.Handles.DrawLine(target + Vector3.down * Tick, target + Vector3.up * Tick);

            if ((target - center).sqrMagnitude > 0.0001f)
            {
                UnityEditor.Handles.DrawDottedLine(center, target, 3f);
            }

            if (!selected) return;

            string label;
            if (_data == null)
            {
                label = "no NodeDataSO";
            }
            else
            {
                label = _data.name + "\nforce " + _data.LaunchForce.ToString("0.#")
                        + "   radius " + _data.Radius.ToString("0.#");

                if (_data.ReuseCooldown > 0f)
                {
                    label += "\ncooldown " + _data.ReuseCooldown.ToString("0.#") + "s";
                }
            }

            UnityEditor.Handles.Label(center + Vector3.up * (radius + 0.25f), label);
        }
#endif
    }
}
