using NestLabs.Player;
using NUnit.Framework;
using UnityEngine;

namespace NestLabs.Tests
{
    /// <summary>
    /// The death-choreography tuning fields are read by <see cref="PlayerDeathSequence"/> and
    /// shipped in a hand-authored <c>PlayerConfig_Default.asset</c> that has no editor to
    /// re-serialize it. This guards the field names and their C# defaults so a rename or a default
    /// drift fails loudly here instead of silently in a playtest.
    /// </summary>
    public sealed class PlayerConfigDeathFieldsTests
    {
        private PlayerConfigSO _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<PlayerConfigSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void DeathJuiceFields_HaveExpectedDefaults()
        {
            Assert.AreEqual(0.4f, _config.DeathShakeAmplitude, 1e-4f);
            Assert.AreEqual(0.35f, _config.DeathShakeDuration, 1e-4f);
            Assert.AreEqual(28f, _config.DeathShakeFrequency, 1e-4f);
            Assert.AreEqual(0.3f, _config.DeathBurstLead, 1e-4f);
            Assert.AreEqual(0.6f, _config.DeathIrisCloseDuration, 1e-4f);
        }
    }
}
