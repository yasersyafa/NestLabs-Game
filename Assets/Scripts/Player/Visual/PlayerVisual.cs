using DG.Tweening;
using GabrielBigardi.SpriteAnimator;
using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// The only class allowed to talk to <see cref="SpriteAnimator"/>. The FSM tells it which state
    /// it entered; it resolves the animation name and plays it. Skins are swapped by prefab variant,
    /// so nothing here is character specific.
    /// </summary>
    public sealed class PlayerVisual : MonoBehaviour
    {
        [SerializeField] private SpriteAnimator _animator;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Transform _squashRoot;

        [Header("Juice")]
        [SerializeField] private float _squashDuration = 0.18f;
        [SerializeField] private Vector3 _jumpSquash = new Vector3(-0.25f, 0.35f, 0f);
        [SerializeField] private Color _hitFlashColor = Color.red;
        [SerializeField] private float _hitFlashDuration = 0.12f;

        private Tween _squashTween;
        private Tween _flashTween;

        private void Reset()
        {
            _animator = GetComponentInChildren<SpriteAnimator>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _squashRoot = transform;
        }

        private void Awake()
        {
            if (_animator == null) _animator = GetComponentInChildren<SpriteAnimator>();
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_squashRoot == null) _squashRoot = transform;

            ValidateSkin();
        }

        private void OnDestroy()
        {
            _squashTween?.Kill();
            _flashTween?.Kill();
        }

        /// <summary>
        /// Fails loudly at startup instead of silently playing nothing when a skin asset is missing
        /// one of the names in <see cref="PlayerAnimId.All"/>.
        /// </summary>
        private void ValidateSkin()
        {
            if (_animator == null || _animator.SpriteAnimationObject == null)
            {
                Debug.LogError($"[PlayerVisual] No SpriteAnimator or skin assigned on '{name}'.", this);
                return;
            }

            foreach (string id in PlayerAnimId.All)
            {
                if (!_animator.HasAnimation(id))
                {
                    Debug.LogError(
                        $"[PlayerVisual] Skin '{_animator.SpriteAnimationObject.name}' is missing the " +
                        $"required animation '{id}'.", this);
                }
            }
        }

        /// <summary>Plays the animation bound to a state. Safe to call every transition.</summary>
        public void PlayForState(PlayerStateId state)
        {
            string id = PlayerAnimId.For(state);
            if (id == null || _animator == null)
            {
                return;
            }

            _animator.PlayIfNotPlaying(id);
        }

        /// <summary>Faces the sprite along a horizontal direction. 0 leaves facing unchanged.</summary>
        public void SetFacing(int direction)
        {
            if (direction == 0 || _renderer == null)
            {
                return;
            }

            _renderer.flipX = direction < 0;
        }

        /// <summary>Squash-and-stretch pop, used on jump and on wall impact. Core DOTween.</summary>
        public void PlaySquash()
        {
            if (_squashRoot == null)
            {
                return;
            }

            _squashTween?.Kill();
            _squashRoot.localScale = Vector3.one;
            _squashTween = _squashRoot
                .DOPunchScale(_jumpSquash, _squashDuration, vibrato: 1, elasticity: 0.4f)
                .SetLink(gameObject);
        }

        /// <summary>Damage flash. Uses SpriteRenderer.DOColor from the DOTween Modules assembly.</summary>
        public void PlayHitFlash()
        {
            if (_renderer == null)
            {
                return;
            }

            _flashTween?.Kill();
            _renderer.color = Color.white;
            _flashTween = _renderer
                .DOColor(_hitFlashColor, _hitFlashDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(gameObject);
        }
    }
}
