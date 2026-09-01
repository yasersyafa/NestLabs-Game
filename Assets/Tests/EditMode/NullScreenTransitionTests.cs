using Cysharp.Threading.Tasks;
using NestLabs.UI;
using NUnit.Framework;
using UnityEngine;

namespace NestLabs.Tests
{
    /// <summary>
    /// The gameplay-only scenes get <see cref="NullScreenTransition"/> instead of the HUD's real one,
    /// so the death choreography must be able to await it and get an already-finished result.
    /// </summary>
    public sealed class NullScreenTransitionTests
    {
        [Test]
        public void CloseAsync_CompletesSynchronously()
        {
            UniTask task = NullScreenTransition.Instance.CloseAsync(Vector2.zero, 1f, default);

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
        }

        [Test]
        public void OpenImmediate_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => NullScreenTransition.Instance.OpenImmediate());
        }
    }
}
