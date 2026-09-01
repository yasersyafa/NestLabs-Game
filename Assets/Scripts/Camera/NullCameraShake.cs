namespace NestLabs
{
    /// <summary>
    /// No-op <see cref="ICameraShake"/>. Injected in scenes with no camera follow component so the
    /// player prefab still runs, degraded to "no shake" rather than a null reference.
    /// </summary>
    public sealed class NullCameraShake : ICameraShake
    {
        public static readonly NullCameraShake Instance = new NullCameraShake();

        private NullCameraShake() { }

        public void Shake(float amplitude, float duration, float frequency) { }
    }
}
