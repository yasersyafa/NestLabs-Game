using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace NestLabs.UI
{
    /// <summary>
    /// Pulses a UI <see cref="Graphic"/> (a TMP label or an <see cref="Image"/>) between two alpha
    /// values on an infinite yoyo loop for as long as the object is enabled: on, off, on, off. Used
    /// for a "Tap to Start" prompt that should keep breathing until the run begins.
    ///
    /// The tween is created in <see cref="OnEnable"/> and killed in <see cref="OnDisable"/>, so an
    /// object toggled with SetActive never leaves a dangling tween writing to a hidden or destroyed
    /// graphic. Same lifetime handling as <see cref="ButtonHoverInvert"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class TextFadePulse : MonoBehaviour
    {
        [SerializeField] private float minAlpha = 0f;
        [SerializeField] private float maxAlpha = 1f;

        // One fade leg; a full off->on->off cycle takes twice this.
        [SerializeField] private float halfCycleDuration = 0.6f;
        [SerializeField] private Ease ease = Ease.InOutSine;

        private Graphic graphic;
        private Tween tween;

        private void Awake() => graphic = GetComponent<Graphic>();

        private void OnEnable()
        {
            if (graphic == null) graphic = GetComponent<Graphic>();

            SetAlpha(maxAlpha);

            // Unscaled: the menu shell can sit at Time.timeScale 0. SetLink so the tween also dies
            // if the GameObject is destroyed without OnDisable running first.
            tween = graphic.DOFade(minAlpha, halfCycleDuration)
                .SetEase(ease)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void OnDisable()
        {
            tween?.Kill();
            tween = null;
            SetAlpha(maxAlpha);
        }

        private void SetAlpha(float a)
        {
            if (graphic == null) return;
            Color c = graphic.color;
            c.a = a;
            graphic.color = c;
        }
    }
}
