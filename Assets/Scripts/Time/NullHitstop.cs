using UnityEngine;

namespace NestLabs
{
    /// <summary>Swallows everything. Default when no container has injected a real one.</summary>
    public sealed class NullHitstop : IHitstop
    {
        public static readonly NullHitstop Instance = new NullHitstop();

        /// <summary>
        /// Falls back to the no-op sink for a missing service, and for a destroyed MonoBehaviour
        /// that `??` cannot see. Begin on a destroyed Hitstop still sets the time scale, but its
        /// Update never runs again, so the dip would never be undone.
        /// </summary>
        public static IHitstop Safe(IHitstop candidate)
        {
            if (candidate is Object obj && obj == null) return Instance;
            return candidate ?? Instance;
        }

        public void Begin(float scale, float unscaledDuration) { }

        public void Cancel() { }
    }
}
