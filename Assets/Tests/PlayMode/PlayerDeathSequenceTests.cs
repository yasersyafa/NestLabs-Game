using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NestLabs.Player;
using NestLabs.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NestLabs.Tests
{
    /// <summary>
    /// The death choreography awaits an iris wipe before it reports done. These checks pin the
    /// contract the HUD depends on: exactly one completion, the collaborators are actually driven,
    /// and a scene-reload cancellation mid-sequence raises nothing.
    /// </summary>
    public sealed class PlayerDeathSequenceTests
    {
        private GameObject _go;
        private PlayerConfigSO _config;
        private RecordingSink _sink;
        private FakeScreenTransition _transition;
        private FakeCameraShake _shake;
        private PlayerDeathSequence _sequence;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("PlayerDeathSequenceUnderTest");
            PlayerMotor motor = _go.AddComponent<PlayerMotor>();
            PlayerSensor sensor = _go.AddComponent<PlayerSensor>();
            PlayerVisual visual = _go.AddComponent<PlayerVisual>();
            PlayerHealth health = _go.AddComponent<PlayerHealth>();
            _go.AddComponent<CircleCollider2D>();
            PlayerNodeSensor nodeSensor = _go.AddComponent<PlayerNodeSensor>();
            _sequence = _go.AddComponent<PlayerDeathSequence>();

            _config = ScriptableObject.CreateInstance<PlayerConfigSO>();
            // Collapse every timed beat so the sequence runs out in a handful of frames.
            _config.DeathSlowScale = 1f;
            _config.DeathBurstLead = 0f;
            _config.DeathIrisCloseDuration = 0f;
            _config.DeathHoldDuration = 0f;
            health.Initialize(_config);

            _sink = new RecordingSink();
            _transition = new FakeScreenTransition();
            _shake = new FakeCameraShake();

            var context = new PlayerContext(
                new PlayerStateMachine(), motor, sensor, nodeSensor, visual, null, _sequence, health,
                NullHitstop.Instance, null, _config, NullPlayerInput.Instance, _sink, _go.transform);
            context.ResetBlackboard();

            _sequence.Initialize(context, null, _shake, _transition);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        [UnityTest]
        public IEnumerator Death_CompletesExactlyOnce_AndDrivesCollaborators()
        {
            _sequence.Play();

            yield return WaitForCompletion();

            Assert.AreEqual(1, _sink.CompletedCount, "completion event fired exactly once");
            Assert.AreEqual(1, _transition.CloseCalls, "iris close was awaited");
            Assert.AreEqual(1, _shake.ShakeCalls, "camera shake was kicked");

            // A few more frames must not produce a second completion.
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(1, _sink.CompletedCount);
        }

        [UnityTest]
        public IEnumerator Death_WithNullCollaborators_StillCompletesOnce()
        {
            _sequence.Initialize(RebuildContext(), null, NullCameraShake.Instance, NullScreenTransition.Instance);

            _sequence.Play();

            yield return WaitForCompletion();

            Assert.AreEqual(1, _sink.CompletedCount);
        }

        [UnityTest]
        public IEnumerator Cancellation_BeforeCompletion_RaisesNothing()
        {
            _sequence.Play();
            yield return null;

            Assert.DoesNotThrow(() => Object.DestroyImmediate(_go));
            _go = null;

            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(0, _sink.CompletedCount);
        }

        private IEnumerator WaitForCompletion()
        {
            for (int i = 0; i < 120 && _sink.CompletedCount == 0; i++)
            {
                yield return null;
            }
        }

        private PlayerContext RebuildContext()
        {
            var context = new PlayerContext(
                new PlayerStateMachine(),
                _go.GetComponent<PlayerMotor>(),
                _go.GetComponent<PlayerSensor>(),
                _go.GetComponent<PlayerNodeSensor>(),
                _go.GetComponent<PlayerVisual>(),
                null,
                _sequence,
                _go.GetComponent<PlayerHealth>(),
                NullHitstop.Instance,
                null,
                _config,
                NullPlayerInput.Instance,
                _sink,
                _go.transform);
            context.ResetBlackboard();
            return context;
        }

        private sealed class FakeScreenTransition : IScreenTransition
        {
            public int CloseCalls;

            public UniTask CloseAsync(Vector2 worldCenter, float duration, CancellationToken ct)
            {
                CloseCalls++;
                return UniTask.CompletedTask;
            }

            public void OpenImmediate() { }
        }

        private sealed class FakeCameraShake : ICameraShake
        {
            public int ShakeCalls;

            public void Shake(float amplitude, float duration, float frequency) => ShakeCalls++;
        }

        private sealed class RecordingSink : IPlayerEventSink
        {
            public int CompletedCount;

            public void StateChanged(PlayerStateId from, PlayerStateId to) { }
            public void Jumped(int fromWallSide) { }
            public void Dashed(int direction) { }
            public void Latched(int wallSide) { }
            public void Hit(int damage, int remainingHealth, Vector2 knockback) { }
            public void Died(Vector2 position) { }

            public void DeathSequenceCompleted() => CompletedCount++;
        }
    }
}
