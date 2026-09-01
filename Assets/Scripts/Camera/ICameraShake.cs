namespace NestLabs
{
    /// <summary>
    /// A one-shot positional camera shake. Kept as a narrow interface so the death choreography can
    /// ask for a jolt without depending on the concrete follow component, and so a container-less
    /// scene falls back to <see cref="NullCameraShake"/> instead of null-referencing.
    /// </summary>
    public interface ICameraShake
    {
        /// <summary>
        /// Kicks a shake that decays to nothing over <paramref name="duration"/> real seconds.
        /// A call while one is running retargets it, it does not stack.
        /// </summary>
        /// <param name="amplitude">Peak offset in world units.</param>
        /// <param name="duration">Real seconds until the shake is fully decayed.</param>
        /// <param name="frequency">Noise sample rate — higher is a busier, buzzier shake.</param>
        void Shake(float amplitude, float duration, float frequency);
    }
}
