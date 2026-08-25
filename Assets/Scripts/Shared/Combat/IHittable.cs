namespace NestLabs.Shared.Combat
{
    /// <summary>
    /// The callback side of a hit: the thing that dealt damage is told the hit actually landed.
    /// Raised only for accepted hits, so implementers never see contacts eaten by i-frames.
    /// </summary>
    public interface IHittable
    {
        void OnHit();
    }
}
