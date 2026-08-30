using System;
using MessagePipe;
using NestLabs.Score;
using TMPro;
using UnityEngine;
using VContainer;

namespace NestLabs.UI
{
    /// <summary>
    /// Fills the game-over panel's score readout from the run that just ended. Pure display, same
    /// contract as <see cref="ScoreHud"/>: it only listens to <see cref="ScoreFinalizedEvent"/>, so
    /// the number it shows is always the exact one the HUD ended the run on, and the panel prefab
    /// itself stays script-free.
    ///
    /// Lives on the always-active HUD root, not on the panel, because the panel starts inactive and
    /// an inactive object gets no injection - the event would be missed. Toggling a child label on
    /// an inactive parent is fine.
    /// </summary>
    public sealed class DiedScorePanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text bestScoreText;

        [Tooltip("Shown only when the finished run set a new best. Optional.")]
        [SerializeField] private GameObject newBestLabel;

        [Header("Format")]
        [SerializeField] private string scoreFormat = "{0}m";
        [SerializeField] private string bestScoreFormat = "BEST {0}m";

        private IDisposable subscriptions;

        [Inject]
        public void Construct(ISubscriber<ScoreFinalizedEvent> scoreFinalized)
        {
            subscriptions = scoreFinalized.Subscribe(OnFinalized);
        }

        private void Awake()
        {
            if (newBestLabel != null) newBestLabel.SetActive(false);
        }

        private void OnDestroy()
        {
            subscriptions?.Dispose();
        }

        private void OnFinalized(ScoreFinalizedEvent e)
        {
            if (finalScoreText != null) finalScoreText.text = string.Format(scoreFormat, e.FinalScore);
            if (bestScoreText != null) bestScoreText.text = string.Format(bestScoreFormat, e.BestScore);
            if (newBestLabel != null) newBestLabel.SetActive(e.IsNewBest);
        }
    }
}
