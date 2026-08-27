namespace NestLabs.Score
{
    /// <summary>
    /// Persistence for the all-time best score. Mirrors the IAudioMuteStore pattern:
    /// ScoreService owns the value, this only reads/writes it across sessions.
    /// </summary>
    public interface IScoreStore
    {
        int LoadBestScore();
        void SaveBestScore(int bestScore);
    }
}
