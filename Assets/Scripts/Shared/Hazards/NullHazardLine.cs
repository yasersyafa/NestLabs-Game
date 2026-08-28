namespace NestLabs.Shared.Hazards
{
    /// <summary>
    /// Reports no line at all. Default when a scene has no fog, so the level system falls back to
    /// its player-relative cull distance instead of null-referencing.
    /// </summary>
    public sealed class NullHazardLine : IHazardLine
    {
        public static readonly NullHazardLine Instance = new NullHazardLine();

        public bool IsActive => false;

        // Negative infinity so any accidental direct comparison keeps everything rather than
        // culling the whole level.
        public float LethalY => float.NegativeInfinity;
    }
}
