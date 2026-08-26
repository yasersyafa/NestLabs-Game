using System;
using MessagePipe;
using NestLabs.Score;
using TMPro;
using UnityEngine;
using VContainer;

namespace NestLabs.UI
{
    /// <summary>
    /// TextMeshPro readout for the live and best score. Pure display: it never touches
    /// ScoreService directly, only the events it publishes, so it can be dropped or swapped
    /// without any other system knowing.
    /// </summary>
    public sealed class ScoreHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestScoreText;

        [Header("Format")]
        [SerializeField] private string scoreFormat = "{0}";
        [SerializeField] private string bestScoreFormat = "BEST {0}";

        private IDisposable subscriptions;

        [Inject]
        public void Construct(
            ISubscriber<ScoreChangedEvent> scoreChanged,
            ISubscriber<ScoreFinalizedEvent> scoreFinalized)
        {
            DisposableBagBuilder bag = DisposableBag.CreateBuilder();
            scoreChanged.Subscribe(e => SetScore(e.CurrentScore)).AddTo(bag);
            scoreFinalized.Subscribe(e => SetFinal(e.FinalScore, e.BestScore)).AddTo(bag);
            subscriptions = bag.Build();
        }

        private void Awake()
        {
            SetScore(0);
        }

        private void OnDestroy()
        {
            subscriptions?.Dispose();
        }

        private void SetScore(int current)
        {
            if (scoreText != null) scoreText.text = string.Format(scoreFormat, current);
        }

        private void SetFinal(int final, int best)
        {
            SetScore(final);
            if (bestScoreText != null) bestScoreText.text = string.Format(bestScoreFormat, best);
        }
    }
}
