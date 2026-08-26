using NUnit.Framework;
using UnityEngine;

namespace NestLabs.Tests
{
    /// <summary>
    /// Hitstop is the only writer of Time.timeScale, so the pause channel has to co-exist with a
    /// running dip without either one stranding the game at the wrong speed.
    /// </summary>
    public sealed class HitstopTests
    {
        private GameObject _go;
        private Hitstop _hitstop;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("HitstopUnderTest");
            _hitstop = _go.AddComponent<Hitstop>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Time.timeScale = 1f;
        }

        [Test]
        public void Pause_FreezesTime_AndUnpauseRestoresFullSpeed()
        {
            _hitstop.SetPaused(true);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            _hitstop.SetPaused(false);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void Pause_DuringADip_ResumesTheDip_NotFullSpeed()
        {
            _hitstop.Begin(0.25f, 1f);
            Assert.AreEqual(0.25f, Time.timeScale, 0.0001f);

            _hitstop.SetPaused(true);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            _hitstop.SetPaused(false);
            Assert.AreEqual(0.25f, Time.timeScale, 0.0001f,
                "The dip was not over, so unpausing must hand it back.");
        }

        [Test]
        public void Begin_WhilePaused_DoesNotUnfreeze()
        {
            _hitstop.SetPaused(true);

            _hitstop.Begin(0.5f, 1f);

            Assert.AreEqual(0f, Time.timeScale, 0.0001f, "A pause outranks a dip.");
        }

        [Test]
        public void Cancel_WhilePaused_LeavesTimeFrozen()
        {
            _hitstop.Begin(0.5f, 1f);
            _hitstop.SetPaused(true);

            _hitstop.Cancel();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            _hitstop.SetPaused(false);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "The cancelled dip must not come back.");
        }
    }
}
