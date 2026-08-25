using DG.Tweening;
using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Afterimages spawned along a grapple pull. The pool and its fade callbacks are built once at
    /// Awake and reused round-robin, so a launch allocates nothing however long it runs.
    /// </summary>
    public sealed class PlayerTrail : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _source;

        [Header("Trail")]
        [Tooltip("Afterimages alive at once. Pool size times spawn interval should cover the longest pull.")]
        [SerializeField] [Range(2, 24)] private int _poolSize = 10;

        [Tooltip("Seconds between afterimages while the trail is running.")]
        [SerializeField] [Min(0.005f)] private float _spawnInterval = 0.02f;

        [Tooltip("Seconds an afterimage takes to fade out.")]
        [SerializeField] [Min(0.01f)] private float _fadeDuration = 0.22f;

        [SerializeField] private Color _startColor = new Color(1f, 1f, 1f, 0.55f);

        [Tooltip("Drawn this many sorting orders behind the player.")]
        [SerializeField] private int _sortingOffset = -1;

        private Transform _holder;
        private SpriteRenderer[] _pool;
        private Tween[] _tweens;
        private TweenCallback[] _onFaded;
        private int _next;
        private bool _running;
        private float _spawnTimer;

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

            // The holder sits outside the player, so it has to be cleaned up by hand.
            if (_holder != null) Destroy(_holder.gameObject);
        }

        /// <summary>Starts emitting. Safe to call when already running.</summary>
        public void Begin()
        {
            if (_source == null) return;

            _running = true;
            _spawnTimer = _spawnInterval;
            Emit();
        }

        /// <summary>Stops emitting. Afterimages already in the air finish fading on their own.</summary>
        public void End()
        {
            _running = false;
        }

        private void LateUpdate()
        {
            if (!_running) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f) return;

            _spawnTimer = _spawnInterval;
            Emit();
        }

        private void BuildPool()
        {
            // Detached from the player on purpose: a parented afterimage would fly along with the
            // player instead of staying where it was stamped.
            var holderGo = new GameObject($"{name}_Trail");
            _holder = holderGo.transform;

            _pool = new SpriteRenderer[_poolSize];
            _tweens = new Tween[_poolSize];
            _onFaded = new TweenCallback[_poolSize];

            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject("Afterimage");
                go.transform.SetParent(_holder, false);
                go.SetActive(false);

                var sr = go.AddComponent<SpriteRenderer>();
                if (_source != null)
                {
                    sr.sharedMaterial = _source.sharedMaterial;
                    sr.sortingLayerID = _source.sortingLayerID;
                    sr.sortingOrder = _source.sortingOrder + _sortingOffset;
                }

                _pool[i] = sr;

                // Captured once here rather than per emit, so Emit stays allocation free.
                int index = i;
                _onFaded[i] = () => _pool[index].gameObject.SetActive(false);
            }
        }

        private void Emit()
        {
            if (_pool == null || _source == null || _source.sprite == null) return;

            SpriteRenderer sr = _pool[_next];
            _tweens[_next]?.Kill();

            Transform src = _source.transform;
            Transform t = sr.transform;
            t.SetPositionAndRotation(src.position, src.rotation);
            t.localScale = src.lossyScale;

            sr.sprite = _source.sprite;
            sr.flipX = _source.flipX;
            sr.flipY = _source.flipY;
            sr.color = _startColor;
            sr.gameObject.SetActive(true);

            _tweens[_next] = sr
                .DOFade(0f, _fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnComplete(_onFaded[_next]);

            _next = (_next + 1) % _pool.Length;
        }
    }
}
