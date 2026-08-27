using UnityEngine;

namespace NestLabs.Score
{
    public sealed class PlayerPrefsScoreStore : IScoreStore
    {
        private const string BestScoreKey = "nestlabs.score.best";

        public int LoadBestScore() => PlayerPrefs.GetInt(BestScoreKey, 0);

        public void SaveBestScore(int bestScore)
        {
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }
    }
}
