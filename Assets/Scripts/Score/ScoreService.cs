using MessagePipe;
using NestLabs.Player;
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

        private float baselineY;
        private float highestY;
        private bool baselineSet;
        private bool runActive;

        public int CurrentScore => data.CurrentScore;
        public int BestScore => data.BestScore;

        [Inject]
        public void Construct(
            ISubscriber<PlayerDiedEvent> died,
            IPublisher<ScoreChangedEvent> scoreChanged,
            IPublisher<ScoreFinalizedEvent> scoreFinalized)
        {
            scoreChangedPublisher = scoreChanged;
            scoreFinalizedPublisher = scoreFinalized;
            died.Subscribe(_ => FinalizeRun());
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
        }

        public void AddBonusPoints(int points)
        {
            if (points == 0 || !runActive) return;
            data.CurrentScore += points;
            if (data.CurrentScore > data.BestScore) data.BestScore = data.CurrentScore;
            scoreChangedPublisher?.Publish(new ScoreChangedEvent(data.CurrentScore));
        }

        private void RecomputeScore()
        {
            float climbed = Mathf.Max(0f, highestY - baselineY);
            data.CurrentScore = Mathf.RoundToInt(climbed * pointsPerUnit);
            if (data.CurrentScore > data.BestScore) data.BestScore = data.CurrentScore;
            scoreChangedPublisher?.Publish(new ScoreChangedEvent(data.CurrentScore));
        }

        private void FinalizeRun()
        {
            runActive = false;
            scoreFinalizedPublisher?.Publish(new ScoreFinalizedEvent(data.CurrentScore, data.BestScore));
        }
    }
}
