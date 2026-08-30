namespace NestLabs.Score
{
    public readonly struct ScoreChangedEvent
    {
        public readonly int CurrentScore;
        public readonly int BestScore;

        public ScoreChangedEvent(int currentScore, int bestScore)
        {
            CurrentScore = currentScore;
            BestScore = bestScore;
        }
    }

    public readonly struct ScoreFinalizedEvent
    {
        public readonly int FinalScore;
        public readonly int BestScore;

        // True only when this run's score beat the best that was stored when the run started, so
        // the game-over panel can show a "new best" badge without re-reading the store itself.
        public readonly bool IsNewBest;

        public ScoreFinalizedEvent(int finalScore, int bestScore, bool isNewBest)
        {
            FinalScore = finalScore;
            BestScore = bestScore;
            IsNewBest = isNewBest;
        }
    }
}
