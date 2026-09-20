namespace Nestlabs.Chunk
{
    // Drives ChunkSpawnRuleSO's difficulty ramp - a chunk's tier maps to a weight multiplier that
    // rises with climbed distance, so Hard layouts (the ones authored with the least spare
    // corridor) only enter the pool once the player has had practice with Easy/Mid first.
    public enum ChunkTier
    {
        Easy,
        Mid,
        Hard
    }
}
