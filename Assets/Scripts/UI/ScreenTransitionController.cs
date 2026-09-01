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
    /// </summary>
    public sealed class ScreenTransitionController : MonoBehaviour, IScreenTransition
    {
        [Tooltip("Full-stretch Image using the NestLabs/UI/IrisClose material. Starts inactive.")]
        [SerializeField] private Image irisImage;

        [Tooltip("Camera the world death point is projected through. Falls back to Camera.main.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Extra radius past the far screen corner so the cover is total. In viewport units.")]
        [SerializeField] private float coverPadding = 0.05f;

        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        private Material _mat;
        private IDisposable _subscription;

        [Inject]
        public void Construct(ISubscriber<GameStateChangedEvent> gameStateChanged)
        {
            // A fresh run or a retry snaps the screen back open. Pause / Death leave it as-is.
            _subscription = gameStateChanged.Subscribe(e =>
            {
                if (e.To == GameState.Play || e.To == GameState.Menu)
                {
                    OpenImmediate();
                }
            });
        }

        private void Awake()
        {
            if (irisImage != null)
            {
                // Clone so SetFloat never touches the project material asset.
                _mat = new Material(irisImage.material);
                irisImage.material = _mat;
            }

            OpenImmediate();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            if (_mat != null) Destroy(_mat);
        }

        public void OpenImmediate()
        {
            if (_mat != null) _mat.SetFloat(RadiusId, 0f);
            if (irisImage != null) irisImage.gameObject.SetActive(false);
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
