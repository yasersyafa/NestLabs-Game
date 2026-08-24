namespace NestLabs.Audio
{
    // Enum keys, not string keys: no hashing at call sites, and AudioService can index a
    // pre-built array by (int)id instead of a per-call dictionary lookup.

    public enum SfxId
    {
        Jump,
        Dash,
        Latch,
        Hit,
        Death,
        ObstacleHit,
        ScoreFinalize,
    }

    public enum MusicId
    {
        Menu,
        Gameplay,
    }
}
