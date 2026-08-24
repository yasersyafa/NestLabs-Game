namespace NestLabs.Score
{
    public readonly struct ScoreChangedEvent
    {
        public readonly int CurrentScore;
        public ScoreChangedEvent(int currentScore) => CurrentScore = currentScore;
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
