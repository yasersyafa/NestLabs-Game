using NestLabs.Player;
using NUnit.Framework;
using UnityEngine;

namespace NestLabs.Tests
{
    /// <summary>
    /// <see cref="PlayerHealth"/> owns the i-frame decision: a hit is rejected — and nothing
    /// changes — while immunity is active or the player is already dead.
    /// </summary>
    public sealed class PlayerHealthTests
    {
        private GameObject _go;
        private PlayerConfigSO _config;
        private PlayerHealth _health;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("HealthUnderTest");
            _health = _go.AddComponent<PlayerHealth>();

            _config = ScriptableObject.CreateInstance<PlayerConfigSO>();
            _config.MaxHealth = 3;
            _config.InvulnerabilityDuration = 0f;
            _health.Initialize(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initialize_SetsCurrentToMaxHealth()
        {
            Assert.AreEqual(3, _health.Current);
            Assert.IsFalse(_health.IsDead);
        }

        [Test]
        public void TryApplyDamage_AcceptedHit_ReducesHealthAndRecordsSource()
        {
            var source = new Vector2(4f, 0f);

            Assert.IsTrue(_health.TryApplyDamage(1, source));
            Assert.AreEqual(2, _health.Current);
            Assert.AreEqual(source, _health.LastDamageSourcePosition);
        }

        [Test]
        public void TryApplyDamage_DuringIFrames_IsRejectedAndChangesNothing()
        {
            _config.InvulnerabilityDuration = 10f;
            _health.Initialize(_config);

            Assert.IsTrue(_health.TryApplyDamage(1, Vector2.zero));
            Assert.IsFalse(_health.TryApplyDamage(1, Vector2.zero), "i-frames reject the second hit");

            Assert.AreEqual(2, _health.Current);
        }

        [Test]
        public void TryApplyDamage_WhenAlreadyDead_IsRejected()
        {
            _health.TryApplyDamage(_config.MaxHealth, Vector2.zero);
            Assert.IsTrue(_health.IsDead);

            Assert.IsFalse(_health.TryApplyDamage(1, Vector2.zero));
        }

        [Test]
        public void LethalDamage_ClampsCurrentToZero()
        {
            _health.TryApplyDamage(99, Vector2.zero);

            Assert.AreEqual(0, _health.Current);
            Assert.IsTrue(_health.IsDead);
        }
    }
}
