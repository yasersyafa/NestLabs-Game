using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NestLabs.Shared.Flow;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace NestLabs.UI
{
    /// <summary>
    /// Drives the iris wipe on a full-stretch <see cref="Image"/> that uses the
    /// <c>NestLabs/UI/IrisClose</c> material: a black disc that blooms out from the death point until
    /// it covers the screen. The death choreography awaits <see cref="CloseAsync"/>; a fresh run
    /// resets it. The material is cloned per instance so the shared asset is never written — same
    /// discipline as <see cref="ButtonHoverInvert"/>.
    ///
    /// Also supports the opening state where the screen starts with an inverted color zone covering the screen,
    /// which shrinks down to the player's position on the first tap.
    /// </summary>
    public sealed class ScreenTransitionController : MonoBehaviour, IScreenTransition
    {
        [Tooltip("Full-stretch Image using the NestLabs/UI/IrisClose material. Starts inactive.")]
        [SerializeField] private Image irisImage;

        [Header("Opening Invert Transition")]
        [Tooltip("Optional full-stretch Image using NestLabs/UI/IrisInvert material. When assigned, covers screen in Menu state and shrinks on Play.")]
        [SerializeField] private Image invertImage;

        [Tooltip("Duration in seconds for the invert circle to shrink down to the player on the first tap.")]
        [SerializeField] private float invertShrinkDuration = 0.8f;

        [Tooltip("Camera the world points are projected through. Falls back to Camera.main.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Extra radius past the far screen corner so the cover is total. In viewport units.")]
        [SerializeField] private float coverPadding = 0.05f;

        [Tooltip("Optional explicit reference to the PlayerBase. If null, cached on demand once.")]
        [SerializeField] private Player.PlayerBase playerRef;

        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        private Material _mat;
        private Material _invertMat;
        private Player.PlayerBase _cachedPlayer;
        private IDisposable _subscription;
        private CancellationTokenSource _invertCts;

        [Inject]
        public void Construct(ISubscriber<GameStateChangedEvent> gameStateChanged)
        {
            // State transitions:
            // - Menu: prepare/show the full invert circle (waiting for tap)
            // - Play: if coming from Menu, shrink the invert circle to player.
            // - Death / Pause: standard handling
            _subscription = gameStateChanged.Subscribe(e =>
            {
                if (e.To == GameState.Menu)
                {
                    OpenImmediate();
                    PrepareInvert();
                }
                else if (e.To == GameState.Play)
                {
                    OpenImmediate();
                    if (e.From == GameState.Menu)
                    {
                        TriggerInvertShrink();
                    }
                    else
                    {
                        HideInvertImmediate();
                    }
                }
            });
        }

        private void Awake()
        {
            if (irisImage != null)
            {
                irisImage.raycastTarget = false;
                // Clone so SetFloat never touches the project material asset.
                _mat = new Material(irisImage.material);
                irisImage.material = _mat;
            }

            if (invertImage != null)
            {
                invertImage.raycastTarget = false;
                _invertMat = new Material(invertImage.material);
                invertImage.material = _invertMat;
            }

            OpenImmediate();
        }

        private void Start()
        {
            // Initial boot is in GameState.Menu
            PrepareInvert();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _invertCts?.Cancel();
            _invertCts?.Dispose();
            if (_mat != null) Destroy(_mat);
            if (_invertMat != null) Destroy(_invertMat);
        }

        public void OpenImmediate()
        {
            if (_mat != null) _mat.SetFloat(RadiusId, 0f);
            if (irisImage != null) irisImage.gameObject.SetActive(false);
        }

        private void HideInvertImmediate()
        {
            _invertCts?.Cancel();
            if (_invertMat != null) _invertMat.SetFloat(RadiusId, 0f);
            if (invertImage != null) invertImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// Puts the invert circle at maximum size covering the whole screen while waiting for the first tap.
        /// </summary>
        private void PrepareInvert()
        {
            if (invertImage == null || _invertMat == null) return;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            Vector2 playerVp = GetPlayerViewportCenter(cam);

            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
            float maxR = MaxCornerDistance(playerVp, aspect) + coverPadding;

            _invertMat.SetVector(CenterId, new Vector4(playerVp.x, playerVp.y, 0f, 0f));
            _invertMat.SetFloat(AspectId, aspect);
            _invertMat.SetFloat(RadiusId, maxR);
            invertImage.gameObject.SetActive(true);
        }

        /// <summary>
        /// Smoothly shrinks the invert circle from full cover down to 0 at the player's position.
        /// </summary>
        private void TriggerInvertShrink()
        {
            if (invertImage == null || _invertMat == null) return;

            _invertCts?.Cancel();
            _invertCts?.Dispose();
            _invertCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            ShrinkInvertAsync(_invertCts.Token).Forget();
        }

        private async UniTaskVoid ShrinkInvertAsync(CancellationToken ct)
        {
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;

            Vector2 center = GetPlayerViewportCenter(cam);
            float startRadius = MaxCornerDistance(center, aspect) + coverPadding;
            float duration = Mathf.Max(0.01f, invertShrinkDuration);

            _invertMat.SetVector(CenterId, new Vector4(center.x, center.y, 0f, 0f));
            _invertMat.SetFloat(AspectId, aspect);
            _invertMat.SetFloat(RadiusId, startRadius);
            invertImage.gameObject.SetActive(true);

            float t = 0f;
            while (t < duration)
            {
                ct.ThrowIfCancellationRequested();
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);

                // Re-evaluate player center in case player moves as jump begins
                center = GetPlayerViewportCenter(cam);
                _invertMat.SetVector(CenterId, new Vector4(center.x, center.y, 0f, 0f));

                // Ease-in: starts gentle and accelerates down to 0
                float factor = Mathf.Pow(1f - k, 2f);
                _invertMat.SetFloat(RadiusId, factor * startRadius);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _invertMat.SetFloat(RadiusId, 0f);
            invertImage.gameObject.SetActive(false);
        }

        private Vector2 GetPlayerViewportCenter(Camera cam)
        {
            if (_cachedPlayer == null)
            {
                _cachedPlayer = playerRef != null ? playerRef : FindAnyObjectByType<Player.PlayerBase>();
            }

            if (_cachedPlayer != null && cam != null)
            {
                Vector3 vp = cam.WorldToViewportPoint(_cachedPlayer.transform.position);
                return new Vector2(vp.x, vp.y);
            }

            return new Vector2(0.5f, 0.5f);
        }

        public async UniTask CloseAsync(Vector2 worldCenter, float duration, CancellationToken ct)
        {
            if (irisImage == null || _mat == null || duration <= 0f)
            {
                return;
            }

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            Vector3 vp = cam != null
                ? cam.WorldToViewportPoint(worldCenter)
                : new Vector3(0.5f, 0.5f, 0f);

            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
            var center = new Vector2(vp.x, vp.y);

            _mat.SetVector(CenterId, new Vector4(center.x, center.y, 0f, 0f));
            _mat.SetFloat(AspectId, aspect);
            _mat.SetFloat(RadiusId, 0f);
            irisImage.gameObject.SetActive(true);

            // The disc must reach the farthest screen corner (plus a margin) to cover everything.
            float target = MaxCornerDistance(center, aspect) + coverPadding;

            float t = 0f;
            while (t < duration)
            {
                ct.ThrowIfCancellationRequested();
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                // Ease-out: the black bursts outward fast, then eases into full cover.
                float eased = 1f - Mathf.Pow(1f - k, 3f);
                _mat.SetFloat(RadiusId, eased * target);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _mat.SetFloat(RadiusId, target);
        }

        /// <summary>
        /// Largest aspect-corrected distance from the centre to any screen corner — the radius the
        /// disc must grow to before it covers the whole frame.
        /// </summary>
        private static float MaxCornerDistance(Vector2 center, float aspect)
        {
            float max = 0f;
            for (int cx = 0; cx <= 1; cx++)
            {
                for (int cy = 0; cy <= 1; cy++)
                {
                    float dx = (cx - center.x) * aspect;
                    float dy = cy - center.y;
                    max = Mathf.Max(max, Mathf.Sqrt(dx * dx + dy * dy));
                }
            }

            return max;
        }
    }
}
