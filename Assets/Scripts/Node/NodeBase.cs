using UnityEngine;

namespace NestLabs.Node
{
    /// <summary>
    /// A grapple point. Holds no player knowledge: it publishes a position and a launch force, and
    /// the player node sensor finds it through the trigger on the Radius child. All tuning comes
    /// from <see cref="NodeDataSO"/> so variants differ by asset, not by script.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NodeBase : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private NodeDataSO _data;

        [Header("Parts")]
        [SerializeField] private CircleCollider2D _radius;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Transform _vfxRoot;

        private float _readyAt;

        public Vector2 Position => transform.position;

        public float LaunchForce => _data != null ? _data.LaunchForce : 0f;

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
            // Keeps the collider the designer sees in the Scene view honest about the asset radius.
            if (_radius == null || _sprite == null || _vfxRoot == null) FindParts();
            ApplyData();
        }

        private void Awake()
        {
            if (_radius == null || _sprite == null || _vfxRoot == null) FindParts();

            if (_data == null)
            {
                Debug.LogError($"[NodeBase] No NodeDataSO assigned on '{name}'.", this);
                enabled = false;
                return;
            }

            ApplyData();
        }

        private void Update()
        {
            // Only runs while spent. A node with no cooldown costs one comparison a frame.
            if (_readyAt <= 0f || Time.time < _readyAt) return;

            _readyAt = 0f;
            if (_sprite != null) _sprite.color = _data.Tint;
        }

        /// <summary>Called by the player the instant a launch starts. Puts the node on cooldown.</summary>
        public void Consume()
        {
            if (_data == null) return;

            if (_data.ReuseCooldown > 0f)
            {
                _readyAt = Time.time + _data.ReuseCooldown;
                if (_sprite != null) _sprite.color = _data.SpentTint;
            }

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
        }
    }
}
