using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NestLabs.UI
{
    /// <summary>
    /// Menu intro, run once on enable: the screen starts fully white, then a hole opens from the
    /// centre and grows past the far corner, uncovering the menu UI (art, logo, buttons) that is
    /// already laid out behind it. The inverse of the death choreography's iris close — same
    /// analytic-disc shader driven per frame, forked to <c>NestLabs/UI/IrisReveal</c> (opaque
    /// outside a growing hole, white instead of black).
    ///
    /// The material is cloned per instance so <see cref="Material.SetFloat(int,float)"/> never
    /// writes the shared project asset — same discipline as <see cref="ScreenTransitionController"/>.
    ///
    /// Self-contained: MenuScene carries no VContainer scope, so references are wired directly in
    /// the scene and there is no injection.
    /// </summary>
    public sealed class MenuIntroSequence : MonoBehaviour
    {
        [Header("White iris reveal")]
        [Tooltip("Full-stretch Image using the NestLabs/UI/IrisReveal material. Starts covering.")]
        [SerializeField] private Image revealOverlay;

        [Tooltip("Seconds the screen holds fully white before the hole starts opening.")]
        [SerializeField] private float holdBeforeReveal = 0.35f;

        [Tooltip("Seconds the hole takes to grow from a point to full screen.")]
        [SerializeField] private float revealDuration = 1.2f;

        [Tooltip("Extra radius past the far screen corner so nothing white is left. Viewport units.")]
        [SerializeField] private float coverPadding = 0.05f;

        [Header("Start button target")]
        // Loaded by name, not build index, so reordering Build Settings can't misroute it.
        [SerializeField] private string gameSceneName = "GameScene";

        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        private Material _mat;

        private void Awake()
        {
            if (revealOverlay != null)
            {
                // Clone so SetFloat never touches the project material asset.
                _mat = new Material(revealOverlay.material);
                revealOverlay.material = _mat;
                Debug.Log($"[MenuIntro] overlay material shader = '{_mat.shader.name}', " +
                          $"hasRadius = {_mat.HasProperty(RadiusId)}", this);
            }
            else
            {
                Debug.LogWarning("[MenuIntro] revealOverlay is not assigned.", this);
            }
        }

        private void OnEnable()
        {
            StartCoroutine(Reveal());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        /// <summary>
        /// Hooked to the Start button's <c>onClick</c>. Sync load: MenuScene is tiny and carries no
        /// container, so there is nothing to stream and no state to keep alive across the swap.
        /// </summary>
        public void LoadGameScene()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private IEnumerator Reveal()
        {
            if (revealOverlay == null || _mat == null)
            {
                yield break;
            }

            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;

            _mat.SetFloat(RadiusId, 0f);
            _mat.SetVector(CenterId, new Vector4(0.5f, 0.5f, 0f, 0f));
            _mat.SetFloat(AspectId, aspect);
            revealOverlay.gameObject.SetActive(true);

            if (holdBeforeReveal > 0f) yield return new WaitForSecondsRealtime(holdBeforeReveal);

            // The hole must reach the farthest screen corner (plus a margin) to uncover everything.
            float target = MaxCornerDistance(aspect) + coverPadding;
            Debug.Log($"[MenuIntro] reveal start: aspect={aspect:F3} target={target:F3} " +
                      $"duration={revealDuration}", this);

            // Burn one frame first: the scene-load hitch makes the first frame's delta huge, so
            // start the wall clock only after it has passed or the grow reads as an instant cut.
            yield return null;

            float startTime = Time.realtimeSinceStartup;
            float k = 0f;
            int frames = 0;
            while (k < 1f)
            {
                k = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / Mathf.Max(revealDuration, 0.0001f));
                // Ease-in-out: gentle start and finish so the iris reads as a deliberate open,
                // not a pop.
                float eased = Mathf.SmoothStep(0f, 1f, k);
                _mat.SetFloat(RadiusId, eased * target);
                frames++;
                yield return null;
            }
            Debug.Log($"[MenuIntro] reveal done in {frames} frames", this);

            _mat.SetFloat(RadiusId, target);
            revealOverlay.gameObject.SetActive(false);
        }

        /// <summary>
        /// Largest aspect-corrected distance from the screen centre to any corner — the radius the
        /// hole must grow to before the white fill is gone from every corner.
        /// </summary>
        private static float MaxCornerDistance(float aspect)
        {
            float dx = 0.5f * aspect;
            const float dy = 0.5f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
