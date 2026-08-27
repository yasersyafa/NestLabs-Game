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

        public ScoreFinalizedEvent(int finalScore, int bestScore)
        {
            FinalScore = finalScore;
            BestScore = bestScore;
        }
    }
}
