using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NestLabs.UI
{
    /// <summary>
    /// No-op <see cref="IScreenTransition"/>. Used in gameplay-only scenes (no HUD prefab) so the
    /// death choreography still completes — it just skips straight past the wipe.
    /// </summary>
    public sealed class NullScreenTransition : IScreenTransition
    {
        public static readonly NullScreenTransition Instance = new NullScreenTransition();

        private NullScreenTransition() { }

        public UniTask CloseAsync(Vector2 worldCenter, float duration, CancellationToken ct)
            => UniTask.CompletedTask;

        public void OpenImmediate() { }
    }
}
