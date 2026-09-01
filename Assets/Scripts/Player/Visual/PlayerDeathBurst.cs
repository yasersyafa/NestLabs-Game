using DG.Tweening;
using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// A one-shot black-and-white shard burst fired the instant the fog takes the player. Pooled and
    /// built once at Awake like <see cref="PlayerTrail"/>, so the burst allocates nothing but its
    /// tween handles. The particle sprite is generated in code — the project has no VFX art and this
    /// keeps the effect a pure script asset.
    /// </summary>
    public sealed class PlayerDeathBurst : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _source;

        [Header("Death Burst")]
        [Tooltip("Shards thrown per burst.")]
        [SerializeField] [Range(4, 48)] private int _count = 16;

        [Tooltip("Per-shard travel speed picked at random, in units/sec.")]
        [SerializeField] private Vector2 _speedRange = new Vector2(3f, 7f);

        [Tooltip("Seconds a shard takes to fly out and fade. Real time — it overlaps the hitstop dip.")]
        [SerializeField] [Min(0.05f)] private float _duration = 0.5f;

        [Tooltip("Shard size at spawn. Shrinks to nothing over the duration.")]
        [SerializeField] [Min(0.01f)] private float _startScale = 0.25f;

        [Tooltip("Half the shards spawn this colour, half spawn its inverse, so the burst reads black-and-white.")]
        [SerializeField] private Color _lightColor = Color.white;

        [Tooltip("Colour every shard fades toward as it dies.")]
        [SerializeField] private Color _endColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Drawn this many sorting orders in front of the player.")]
        [SerializeField] private int _sortingOffset = 20;

        [SerializeField] [Range(8, 64)] private int _poolSize = 24;

        private Transform _holder;
        private SpriteRenderer[] _pool;
        private Tween[] _tweens;
        private TweenCallback[] _onDone;
        private Sprite _shardSprite;
        private int _next;

        private void Reset()
        {
            _source = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (_source == null) _source = GetComponentInChildren<SpriteRenderer>();
            BuildPool();
        }

        private void OnDestroy()
        {
            if (_tweens != null)
            {
                for (int i = 0; i < _tweens.Length; i++) _tweens[i]?.Kill();
            }

            // The holder sits outside the player so shards stay stamped in world space; clean it up.
            if (_holder != null) Destroy(_holder.gameObject);
            if (_shardSprite != null)
            {
                Destroy(_shardSprite.texture);
                Destroy(_shardSprite);
            }
        }

        /// <summary>Throws one burst of shards from a world point. Safe to call once per death.</summary>
        public void Play(Vector3 worldPos)
        {
            if (_pool == null || _shardSprite == null) return;

            float baseAngle = Random.value * Mathf.PI * 2f;

            for (int i = 0; i < _count; i++)
            {
                SpriteRenderer sr = _pool[_next];
                _tweens[_next]?.Kill();
                // Clear the move / scale / colour tweens from a prior burst on this shard.
                sr.transform.DOKill();
                sr.DOKill();

                // Even fan plus a little jitter so the ring does not look mechanical.
                float angle = baseAngle + (i / (float)_count) * Mathf.PI * 2f
                              + Random.Range(-0.2f, 0.2f);
                var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                float speed = Random.Range(_speedRange.x, _speedRange.y);

                Transform t = sr.transform;
                t.position = worldPos;
                t.localScale = Vector3.one * _startScale;

                sr.sprite = _shardSprite;
                // Alternate light / inverted-light so the burst is monochrome but has contrast.
                sr.color = (i & 1) == 0
                    ? _lightColor
                    : new Color(1f - _lightColor.r, 1f - _lightColor.g, 1f - _lightColor.b, _lightColor.a);
                sr.gameObject.SetActive(true);

                // Scale and colour tweens die with the object via SetLink; the move tween is the
                // handle we track and hang the recycle callback on.
                t.DOScale(0f, _duration).SetEase(Ease.InQuad).SetUpdate(true).SetLink(gameObject);
                sr.DOColor(_endColor, _duration).SetEase(Ease.InQuad).SetUpdate(true).SetLink(gameObject);

                _tweens[_next] = t
                    .DOMove(worldPos + dir * (speed * _duration), _duration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetLink(gameObject)
                    .OnComplete(_onDone[_next]);

                _next = (_next + 1) % _pool.Length;
            }
        }

        private void BuildPool()
        {
            _shardSprite = BuildShardSprite();

            var holderGo = new GameObject($"{name}_DeathBurst");
            _holder = holderGo.transform;

            _pool = new SpriteRenderer[_poolSize];
            _tweens = new Tween[_poolSize];
            _onDone = new TweenCallback[_poolSize];

            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject("Shard");
                go.transform.SetParent(_holder, false);
                go.SetActive(false);

                var sr = go.AddComponent<SpriteRenderer>();
                if (_source != null)
                {
                    sr.sortingLayerID = _source.sortingLayerID;
                    sr.sortingOrder = _source.sortingOrder + _sortingOffset;
                }

                _pool[i] = sr;

                int index = i;
                _onDone[i] = () => _pool[index].gameObject.SetActive(false);
            }
        }

        private static Sprite BuildShardSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            float half = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // Soft-edged disc: solid to ~60% radius, feathered to the rim.
                    float a = Mathf.Clamp01(1f - Mathf.InverseLerp(0.6f, 1f, d));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            Sprite sprite = Sprite.Create(
                tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
