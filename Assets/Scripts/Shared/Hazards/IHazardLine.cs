namespace NestLabs.Shared.Hazards
{
    /// <summary>
    /// The rising lethal line that chases the player up the shaft. Lives in Shared so the level
    /// system can read it: the fog itself is in NestLabs.Runtime, which Nestlabs.Level is not
    /// allowed to reference.
    /// </summary>
    public interface IHazardLine
    {
        /// <summary>True while the line is live and climbing.</summary>
        bool IsActive { get; }

        /// <summary>
        /// World Y of the top of the lethal volume. Anything below this can never be reached
        /// again, which is the only safe cue for recycling level content.
        /// </summary>
        float LethalY { get; }
    }
}
