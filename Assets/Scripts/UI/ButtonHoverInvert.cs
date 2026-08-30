using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NestLabs.UI
{
    /// <summary>
    /// Fades a button to its photographic negative while the pointer is over it. Hover only:
    /// EventSystem selection is deliberately ignored, otherwise a mouse click leaves the button
    /// stuck inverted until something else is selected.
    /// The HUD art is monochrome (black panel, white outline, white icon/label), so a
    /// straight <see cref="Selectable.Transition.ColorTint"/> can never lighten the black fill.
    /// The background <see cref="Image"/> gets a cloned <c>NestLabs/UI/Invert</c> material whose
    /// <c>_InvertAmount</c> is driven 0..1; the child graphics (icon, TMP label) are solid white,
    /// so lerping their <see cref="Graphic.color"/> toward the negative is the same effect without
    /// a second shader.
    ///
    /// Added at runtime by <see cref="HudPanelController"/> so the panel prefabs stay script-free.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class ButtonHoverInvert : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Material invertTemplate;
        [SerializeField] private float fadeDuration = 0.1f;

        private static readonly int InvertAmount = Shader.PropertyToID("_InvertAmount");

        private Image background;
        private Graphic[] flipTargets;
        private Color[] originalColors;
        private Material materialInstance;

        private bool pointerOn;
        private float amount;
        private Tween tween;
        private bool cached;

        /// <summary>
        /// Called by <see cref="HudPanelController"/> right after <c>AddComponent</c>, i.e. after
        /// this component's <see cref="Awake"/> has already run. Supplies the shared invert
        /// material to clone from and kicks the first material assignment.
        /// </summary>
        public void Configure(Material template, float duration = 0.1f)
        {
            invertTemplate = template;
            fadeDuration = duration;
            EnsureCached();
            EnsureMaterial();
        }

        private void Awake() => EnsureCached();

        private void OnEnable()
        {
            // A panel re-shown via SetActive keeps whatever colours it had when hidden. Start clean.
            EnsureMaterial();
            pointerOn = false;
            KillTween();
            Apply(0f);
        }

        private void OnDisable() => KillTween();

        private void OnDestroy()
        {
            KillTween();
            if (materialInstance != null) Destroy(materialInstance);
        }

        public void OnPointerEnter(PointerEventData _) { pointerOn = true; Refresh(); }
        public void OnPointerExit(PointerEventData _) { pointerOn = false; Refresh(); }

        private void EnsureCached()
        {
            if (cached) return;
            cached = true;

            background = GetComponent<Image>();

            var all = GetComponentsInChildren<Graphic>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != background) count++;

            flipTargets = new Graphic[count];
            originalColors = new Color[count];
            int w = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == background) continue;
                flipTargets[w] = all[i];
                originalColors[w] = all[i].color;
                w++;
            }
        }

        private void EnsureMaterial()
        {
            if (materialInstance != null || invertTemplate == null || background == null) return;
            materialInstance = new Material(invertTemplate);
            background.material = materialInstance;
            materialInstance.SetFloat(InvertAmount, amount);
        }

        private void Refresh()
        {
            if (!isActiveAndEnabled) return;
            AnimateTo(pointerOn ? 1f : 0f);
        }

        private void AnimateTo(float target)
        {
            KillTween();

            if (fadeDuration <= 0f)
            {
                Apply(target);
                return;
            }

            // Unscaled: the Pause and Death panels are shown while Time.timeScale can be 0.
            tween = DOTween.To(() => amount, Apply, target, fadeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void Apply(float t)
        {
            amount = t;

            if (materialInstance != null) materialInstance.SetFloat(InvertAmount, t);

            if (flipTargets == null) return;
            for (int i = 0; i < flipTargets.Length; i++)
            {
                if (flipTargets[i] == null) continue;
                Color o = originalColors[i];
                flipTargets[i].color = Color.Lerp(o, new Color(1f - o.r, 1f - o.g, 1f - o.b, o.a), t);
            }
        }

        private void KillTween()
        {
            tween?.Kill();
            tween = null;
        }
    }
}
