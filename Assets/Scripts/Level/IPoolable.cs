using System;

namespace Nestlabs.Level
{
    // Implemented by spawned components that need to redo Start()-style setup on reuse (Unity
    // never calls Start() again on a reactivated pooled instance) or reset per-use state on
    // despawn. Components with nothing to reset (e.g. WallTerrain) simply don't implement this -
    // SpawnRuleContext's null-conditional call is a no-op for them.
    public interface IPoolable
    {
        // releaseSelf is non-null only for components that terminate themselves on a timer
        // (e.g. ProjectileObstacle); everything else ignores it and is despawned externally.
        void OnSpawned(Action releaseSelf);
        void OnDespawned();
    }
}
