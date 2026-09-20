namespace NestLabs.Shared.Culling
{
    // Implemented by spawned components whose per-frame cosmetic/CPU work (marker re-pinning,
    // tween playback, warning UI tracking) is safe to skip while off-screen. Lives in Shared,
    // mirroring IPoolable's placement in Level, so Nestlabs.Level can call through it from
    // SpawnRuleContext without Level referencing Wall/Obstacle/Node concrete types. Components
    // with nothing to gate simply don't implement it - the caller's TryGetComponent is a no-op.
    public interface IScreenVisibility
    {
        bool IsScreenVisible { get; }

        void SetScreenVisible(bool visible);
    }
}
