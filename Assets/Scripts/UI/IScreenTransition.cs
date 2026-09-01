using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NestLabs.UI
{
    /// <summary>
    /// A full-screen wipe the death choreography can await. Narrow on purpose: the player side only
    /// needs "close the screen down over this world point", the HUD owns how it looks. A
    /// container-less scene falls back to <see cref="NullScreenTransition"/>.
    /// </summary>
    public interface IScreenTransition
    {
        /// <summary>
        /// Runs the iris closing from a circle centred on <paramref name="worldCenter"/> until the
        /// screen is fully covered. Real time, so a lingering hitstop dip does not stretch it.
        /// Cancellation (a scene reload) surfaces as an <see cref="System.OperationCanceledException"/>.
        /// </summary>
        UniTask CloseAsync(Vector2 worldCenter, float duration, CancellationToken ct);

        /// <summary>Snaps the screen back to fully open with no animation. Called on a fresh run.</summary>
        void OpenImmediate();
    }
}
