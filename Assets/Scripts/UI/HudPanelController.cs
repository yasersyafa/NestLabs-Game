using System;
using System.Collections;
using DG.Tweening;
using MessagePipe;
using NestLabs.Player;
using NestLabs.Shared.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace NestLabs.UI
{
    /// <summary>
    /// Shows and hides the overlay panels from flow state, and routes the UI buttons back into
    /// <see cref="IGameStateService"/>. The only place UI knows about game flow: the panels
    /// themselves are plain prefabs with no scripts, so a panel can be restyled or replaced
    /// without touching any system.
    /// </summary>
    public sealed class HudPanelController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject overlay;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject diedPanel;

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseExitButton;
        [SerializeField] private Button creditButton;
        [SerializeField] private Button creditsExitButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        [Header("Hover")]
        [Tooltip("NestLabs/UI/Invert material. Cloned per button by ButtonHoverInvert.")]
        [SerializeField] private Material buttonInvertMaterial;
        [SerializeField] private float hoverFadeDuration = 0.1f;

        [Header("Death panel")]
        [Tooltip("Faded in when the death choreography finishes. Optional — without it the panel just pops.")]
        [SerializeField] private CanvasGroup diedGroup;
        [SerializeField] private float diedFadeDuration = 0.3f;
        [Tooltip("Safety net: show the panel this many unscaled seconds after death even if the sequence-complete event never arrives.")]
        [SerializeField] private float diedPanelMaxDelay = 3.5f;

        [Tooltip("The Died panel's RectTransform. Slid down from off-screen when the panel is revealed. Optional.")]
        [SerializeField] private RectTransform diedPanelRect;
        [SerializeField] private float diedSlideDuration = 0.4f;
        [SerializeField] private Ease diedSlideEase = Ease.OutCubic;
        [Tooltip("anchoredPosition.y the panel starts from, above the screen, before sliding to 0.")]
        [SerializeField] private float diedSlideFromY = 1918f;

        private IGameStateService gameState = NullGameStateService.Instance;
        private IPlayerInput input;
        private IDisposable subscriptions;

        // The game-over panel is held back until the death choreography raises its completion event
        // (or the fallback timer fires), so it never covers the sequence.
        private bool deathPending;
        private Coroutine deathFallback;
        private Tween diedFadeTween;
        private Tween diedSlideTween;

        // Credits is a sub-view of Pause, not a GameState, so it needs its own flag. Cleared on
        // every transition out of Pause so reopening the menu never lands on the credits page.
        private bool creditsOpen;

        [Inject]
        public void Construct(
            IGameStateService gameState,
            IPlayerInput input,
            ISubscriber<GameStateChangedEvent> gameStateChanged,
            ISubscriber<PlayerDeathSequenceCompletedEvent> deathSequenceCompleted)
        {
            this.gameState = gameState ?? NullGameStateService.Instance;
            this.input = input;

            DisposableBagBuilder bag = DisposableBag.CreateBuilder();
            gameStateChanged.Subscribe(e => HandleGameStateChanged(e.To)).AddTo(bag);
            deathSequenceCompleted.Subscribe(_ => ShowDiedPanel()).AddTo(bag);
            subscriptions = bag.Build();
        }

        private void Start()
        {
            AddListener(pauseButton, () => gameState.Pause());
            AddListener(resumeButton, ResumeRun);
            AddListener(pauseExitButton, ResumeRun);
            AddListener(creditButton, () => SetCreditsOpen(true));
            AddListener(creditsExitButton, () => SetCreditsOpen(false));
            AddListener(retryButton, ReloadScene);
            AddListener(homeButton, ReloadScene);

            EnsureHover(pauseButton);
            EnsureHover(resumeButton);
            EnsureHover(pauseExitButton);
            EnsureHover(creditButton);
            EnsureHover(creditsExitButton);
            EnsureHover(retryButton);
            EnsureHover(homeButton);

            // The broker is not buffered, so the state that was set before this component woke up
            // never arrives as an event. Read it directly for the opening layout.
            Refresh();
        }

        private void OnDestroy()
        {
            subscriptions?.Dispose();

            if (deathFallback != null) StopCoroutine(deathFallback);
            diedFadeTween?.Kill();
            diedSlideTween?.Kill();

            RemoveListeners(pauseButton);
            RemoveListeners(resumeButton);
            RemoveListeners(pauseExitButton);
            RemoveListeners(creditButton);
            RemoveListeners(creditsExitButton);
            RemoveListeners(retryButton);
            RemoveListeners(homeButton);
        }

        private void HandleGameStateChanged(GameState to)
        {
            if (to != GameState.Pause) creditsOpen = false;

            if (input != null)
            {
                // Menu counts as playable input: IdleState's tap is what starts the run.
                input.Enabled = to == GameState.Menu || to == GameState.Play;

                // Drops the press that opened or closed a panel, so it is not still buffered and
                // spent as a wall jump the moment the run resumes.
                input.ClearTap();
            }

            Refresh();
        }

        private void Refresh()
        {
            GameState current = gameState.Current;
            bool paused = current == GameState.Pause;
            bool died = current == GameState.Death;

            SetActive(pausePanel, paused && !creditsOpen);
            SetActive(creditsPanel, paused && creditsOpen);

            // No pausing out of a death, and the button sits under the deferred panel anyway.
            if (pauseButton != null) pauseButton.interactable = !died;

            if (died)
            {
                BeginDeathPanelDeferred();
                return;
            }

            // Left the death state (a retry/restart): drop any pending reveal.
            deathPending = false;
            if (deathFallback != null) { StopCoroutine(deathFallback); deathFallback = null; }
            diedFadeTween?.Kill();
            diedSlideTween?.Kill();
            if (diedPanelRect != null)
            {
                Vector2 rp = diedPanelRect.anchoredPosition;
                diedPanelRect.anchoredPosition = new Vector2(rp.x, 0f);
            }

            SetActive(overlay, paused);
            SetActive(diedPanel, false);
        }

        /// <summary>
        /// Death just landed: keep the panel hidden and arm a fallback timer. The panel is revealed
        /// by <see cref="ShowDiedPanel"/>, driven by the choreography's completion event.
        /// </summary>
        private void BeginDeathPanelDeferred()
        {
            if (deathPending || (diedPanel != null && diedPanel.activeSelf)) return;

            deathPending = true;
            if (deathFallback != null) StopCoroutine(deathFallback);
            deathFallback = StartCoroutine(DeathPanelFallback());
        }

        private IEnumerator DeathPanelFallback()
        {
            yield return new WaitForSecondsRealtime(diedPanelMaxDelay);
            deathFallback = null;
            ShowDiedPanel();
        }

        /// <summary>Reveals the game-over panel, fading it in when a CanvasGroup is wired. Idempotent.</summary>
        private void ShowDiedPanel()
        {
            if (gameState.Current != GameState.Death) return;
            if (diedPanel != null && diedPanel.activeSelf) return;

            deathPending = false;
            if (deathFallback != null) { StopCoroutine(deathFallback); deathFallback = null; }

            SetActive(overlay, true);
            SetActive(diedPanel, true);

            if (diedGroup != null)
            {
                diedFadeTween?.Kill();
                diedGroup.alpha = 0f;
                diedFadeTween = diedGroup
                    .DOFade(1f, diedFadeDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }

            // Slide the card in from above the frame. Unscaled — the panel is shown while
            // Time.timeScale can be 0 (a lingering hitstop dip, or Pause).
            if (diedPanelRect != null)
            {
                diedSlideTween?.Kill();
                Vector2 p = diedPanelRect.anchoredPosition;
                diedPanelRect.anchoredPosition = new Vector2(p.x, diedSlideFromY);
                diedSlideTween = diedPanelRect
                    .DOAnchorPosY(0f, diedSlideDuration)
                    .SetEase(diedSlideEase)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
        }

        private void ResumeRun()
        {
            creditsOpen = false;
            gameState.Resume();
        }

        private void SetCreditsOpen(bool open)
        {
            creditsOpen = open;
            Refresh();
        }

        private static void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Attached at runtime, not baked into the panel prefabs, so the panels stay script-free
        // and the hover effect is opt-in with a single serialized material reference.
        private void EnsureHover(Button button)
        {
            if (button == null || buttonInvertMaterial == null) return;
            if (button.TryGetComponent<ButtonHoverInvert>(out _)) return;

            ButtonHoverInvert hover = button.gameObject.AddComponent<ButtonHoverInvert>();
            hover.Configure(buttonInvertMaterial, hoverFadeDuration);

            // The invert replaces the near-invisible grey ColorTint highlight entirely.
            button.transition = Selectable.Transition.None;
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void RemoveListeners(Button button)
        {
            if (button != null) button.onClick.RemoveAllListeners();
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }
    }
}
