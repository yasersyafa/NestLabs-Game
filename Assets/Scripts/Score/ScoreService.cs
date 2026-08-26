using System;
using MessagePipe;
using NestLabs.Player;
using NestLabs.Shared.Flow;
using UnityEngine;
using VContainer;

namespace NestLabs.Score
{
    public class ScoreService : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBase player;

        [Header("Scoring")]
        [Tooltip("Points awarded per world unit climbed above baseline.")]
        [SerializeField] private float pointsPerUnit = 1f;

        private readonly ScoreData data = new();
        private IPublisher<ScoreChangedEvent> scoreChangedPublisher;
        private IPublisher<ScoreFinalizedEvent> scoreFinalizedPublisher;
        private IDisposable subscriptions;

        private float baselineY;
        private float highestY;
        private bool baselineSet;
        private bool runActive;

        public int CurrentScore => data.CurrentScore;
        public int BestScore => data.BestScore;

        [Inject]
        public void Construct(
            IGameStateService gameState,
            ISubscriber<PlayerDiedEvent> died,
            ISubscriber<GameStateChangedEvent> gameStateChanged,
            IPublisher<ScoreChangedEvent> scoreChanged,
            IPublisher<ScoreFinalizedEvent> scoreFinalized)
        {
            scoreChangedPublisher = scoreChanged;
            scoreFinalizedPublisher = scoreFinalized;

            DisposableBagBuilder bag = DisposableBag.CreateBuilder();
            died.Subscribe(_ => FinalizeRun()).AddTo(bag);
            gameStateChanged.Subscribe(HandleGameStateChanged).AddTo(bag);
            subscriptions = bag.Build();

            // GameStateService starts life already in Play (no menu flow yet), so no
            // GameStateChangedEvent will ever fire for that first run. Pick it up directly.
            if (gameState.IsPlaying) ResetRun();
        }

        private void OnDestroy()
        {
            subscriptions?.Dispose();
        }

        private void HandleGameStateChanged(GameStateChangedEvent e)
        {
            switch (e.To)
            {
                case GameState.Play:
                    // Resuming from Pause keeps the current run's score; anything else (Menu or
                    // Death) starting Play is a fresh run.
                    if (e.From == GameState.Pause) runActive = true;
                    else ResetRun();
                    break;
                case GameState.Pause:
                    runActive = false;
                    break;
            }
        }

        private void Update()
        {
            if (!runActive || player == null || player.Context == null) return;

            float y = player.Context.Transform.position.y;
            if (!baselineSet)
            {
                baselineY = highestY = y;
                baselineSet = true;
                return;
            }

            if (y > highestY)
            {
                highestY = y;
                RecomputeScore();
            }
        }

        [ContextMenu("Reset Run")]
        public void ResetRun()
        {
            runActive = true;
            baselineSet = false;
            data.CurrentScore = 0;
            scoreChangedPublisher?.Publish(new ScoreChangedEvent(0));

            #if UNITY_EDITOR
            Debug.Log($"[ScoreService] ResetRun -> CurrentScore=0, BestScore= {data.BestScore}");
            #endif
        }

        public void AddBonusPoints(int points)
        {
            if (points == 0 || !runActive) return;
            data.CurrentScore += points;
            if (data.CurrentScore > data.BestScore) data.BestScore = data.CurrentScore;
            scoreChangedPublisher?.Publish(new ScoreChangedEvent(data.CurrentScore));

            #if UNITY_EDITOR
            Debug.Log($"[ScoreService] AddBonusPoints({points}) -> CurrentScore={data.CurrentScore}, BestScore={data.BestScore}");
            #endif
        }

        private void RecomputeScore()
        {
            float climbed = Mathf.Max(0f, highestY - baselineY);
            data.CurrentScore = Mathf.RoundToInt(climbed * pointsPerUnit);
            if (data.CurrentScore > data.BestScore) data.BestScore = data.CurrentScore;
            scoreChangedPublisher?.Publish(new ScoreChangedEvent(data.CurrentScore));

            #if UNITY_EDITOR
            Debug.Log($"[ScoreService] RecomputeScore -> baselineY={baselineY:F2}, highestY={highestY:F2}, climbed={climbed:F2}, CurrentScore={data.CurrentScore}, BestScore={data.BestScore}");
            #endif
        }

        private void FinalizeRun()
        {
            runActive = false;
            scoreFinalizedPublisher?.Publish(new ScoreFinalizedEvent(data.CurrentScore, data.BestScore));

            #if UNITY_EDITOR
            Debug.Log($"[ScoreService] FinalizeRun -> FinalScore={data.CurrentScore}, BestScore={data.BestScore}");
            #endif
        }
    }
}
