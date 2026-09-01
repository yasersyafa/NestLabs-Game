using NestLabs.Player;
using NUnit.Framework;
using UnityEngine;

namespace NestLabs.Tests
{
    /// <summary>
    /// The Dead state freezes the body, fires the Died notification, and kicks the death
    /// choreography. It must also survive a null <see cref="PlayerDeathSequence"/> (bare prefab /
    /// test rig).
    /// </summary>
    public sealed class DeadStateTests
    {
        private GameObject _go;
        private PlayerConfigSO _config;
        private RecordingSink _sink;
        private PlayerContext _context;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("DeadStateUnderTest");
            PlayerMotor motor = _go.AddComponent<PlayerMotor>();
            PlayerSensor sensor = _go.AddComponent<PlayerSensor>();
            PlayerVisual visual = _go.AddComponent<PlayerVisual>();
            PlayerHealth health = _go.AddComponent<PlayerHealth>();
            _go.AddComponent<CircleCollider2D>();
            PlayerNodeSensor nodeSensor = _go.AddComponent<PlayerNodeSensor>();

            _config = ScriptableObject.CreateInstance<PlayerConfigSO>();
            health.Initialize(_config);

            _sink = new RecordingSink();
            _context = new PlayerContext(
                new PlayerStateMachine(), motor, sensor, nodeSensor, visual, null, null, health,
                NullHitstop.Instance, null, _config,
                NullPlayerInput.Instance, _sink, _go.transform);
            _context.ResetBlackboard();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Enter_FiresTheDiedNotificationAtThePlayerPosition()
        {
            _go.transform.position = new Vector3(2f, 5f, 0f);

            new DeadState(_context).Enter();

            Assert.IsTrue(_sink.DidDie);
            Assert.AreEqual(new Vector2(2f, 5f), _sink.DiedPosition);
        }

        [Test]
        public void Enter_WithNoDeathSequenceWired_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new DeadState(_context).Enter());
        }

        private sealed class RecordingSink : IPlayerEventSink
        {
            public bool DidDie;
            public Vector2 DiedPosition;

            public void StateChanged(PlayerStateId from, PlayerStateId to) { }
            public void Jumped(int fromWallSide) { }
            public void Dashed(int direction) { }
            public void Latched(int wallSide) { }
            public void Hit(int damage, int remainingHealth, Vector2 knockback) { }

            public void Died(Vector2 position)
            {
                DidDie = true;
                DiedPosition = position;
            }

            public void DeathSequenceCompleted() { }
        }
    }
}
