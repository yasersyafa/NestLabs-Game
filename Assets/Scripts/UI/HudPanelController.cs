using System;
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

        private IGameStateService gameState = NullGameStateService.Instance;
        private IPlayerInput input;
        private IDisposable subscriptions;

        // Credits is a sub-view of Pause, not a GameState, so it needs its own flag. Cleared on
        // every transition out of Pause so reopening the menu never lands on the credits page.
        private bool creditsOpen;

        [Inject]
        public void Construct(
            IGameStateService gameState,
            IPlayerInput input,
            ISubscriber<GameStateChangedEvent> gameStateChanged)
        {
            this.gameState = gameState ?? NullGameStateService.Instance;
            this.input = input;

            DisposableBagBuilder bag = DisposableBag.CreateBuilder();
            gameStateChanged.Subscribe(e => HandleGameStateChanged(e.To)).AddTo(bag);
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

            // The broker is not buffered, so the state that was set before this component woke up
            // never arrives as an event. Read it directly for the opening layout.
            Refresh();
        }

        private void OnDestroy()
        {
            subscriptions?.Dispose();

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

            SetActive(overlay, paused || died);
            SetActive(pausePanel, paused && !creditsOpen);
            SetActive(creditsPanel, paused && creditsOpen);
            SetActive(diedPanel, died);
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
