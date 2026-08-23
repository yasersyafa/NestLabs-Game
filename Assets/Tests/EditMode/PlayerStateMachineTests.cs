using System.Collections.Generic;
using NestLabs.Player;
using NUnit.Framework;
using UnityEngine;

namespace NestLabs.Tests
{
    /// <summary>
    /// Locks down the FSM plumbing: the contextual-tap table, tap buffering, and Enter/Exit
    /// ordering. Feature bodies are still stubs, so nothing here asserts on physics.
    /// </summary>
    public sealed class PlayerStateMachineTests
    {
        private GameObject _go;
        private PlayerConfigSO _config;
        private FakePlayerInput _input;
        private PlayerStateMachine _fsm;
        private PlayerContext _context;
        private PlayerSensor _sensor;

        /// <summary>Stages wall contact so wall-dependent states behave as they would in a scene.</summary>
        private void SenseWall(int side)
        {
            _sensor.Current = new PlayerSense(
                side,
                false,
                0.05f,
                side < 0 ? PlayerCollisionFlags.WallLeft : PlayerCollisionFlags.WallRight);
        }

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("PlayerUnderTest");

            PlayerMotor motor = _go.AddComponent<PlayerMotor>();
            PlayerSensor sensor = _go.AddComponent<PlayerSensor>();
            _sensor = sensor;
            PlayerVisual visual = _go.AddComponent<PlayerVisual>();
            PlayerHealth health = _go.AddComponent<PlayerHealth>();

            _config = ScriptableObject.CreateInstance<PlayerConfigSO>();
            health.Initialize(_config);

            _input = new FakePlayerInput();
            _fsm = new PlayerStateMachine();
            _context = new PlayerContext(
                _fsm, motor, sensor, visual, health, _config, _input,
                NullPlayerEventSink.Instance, _go.transform);
            _context.ResetBlackboard();

            _fsm.Register(new LatchState(_context));
            _fsm.Register(new SlideState(_context));
            _fsm.Register(new JumpState(_context));
            _fsm.Register(new FallState(_context));
            _fsm.Register(new DashState(_context));
            _fsm.Register(new HitState(_context));
            _fsm.Register(new DeadState(_context));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        // --- Contextual tap table --------------------------------------------------------

        [TestCase(PlayerStateId.Latch, PlayerStateId.Jump)]
        [TestCase(PlayerStateId.Slide, PlayerStateId.Jump)]
        [TestCase(PlayerStateId.Jump, PlayerStateId.Dash)]
        [TestCase(PlayerStateId.Fall, PlayerStateId.Dash)]
        public void Tap_ResolvesPerTable(PlayerStateId from, PlayerStateId expected)
        {
            _fsm.Initialize(_context, from);
            // The airborne cases must read as a dash, not as a coyote wall jump.
            _context.LastWallSide = 0;
            _context.LastWallExitTime = float.NegativeInfinity;

            _input.TapPending = true;
            _fsm.Tick(0f);

            Assert.AreEqual(expected, _fsm.CurrentId);
        }

        [TestCase(PlayerStateId.Dash)]
        [TestCase(PlayerStateId.Hit)]
        [TestCase(PlayerStateId.Dead)]
        public void Tap_IsSwallowed_ByUninterruptibleStates(PlayerStateId state)
        {
            _fsm.Initialize(_context, state);

            _input.TapPending = true;
            _fsm.Tick(0f);

            Assert.AreEqual(state, _fsm.CurrentId);
        }

        [Test]
        public void Tap_SwallowedByDash_StaysBufferedForTheNextState()
        {
            _fsm.Initialize(_context, PlayerStateId.Dash);

            _input.TapPending = true;
            _fsm.Tick(0f);

            Assert.IsTrue(_input.TapPending, "Dash must not consume the tap — it has to survive the dash.");
        }

        [Test]
        public void Tap_InAir_IsIgnored_WhenNoDashChargeRemains()
        {
            _fsm.Initialize(_context, PlayerStateId.Fall);
            _context.LastWallSide = 0;
            _context.DashChargesRemaining = 0;

            _input.TapPending = true;
            _fsm.Tick(0f);

            Assert.AreEqual(PlayerStateId.Fall, _fsm.CurrentId);
            Assert.IsTrue(_input.TapPending, "A refused tap stays buffered until a dash is legal again.");
        }

        [Test]
        public void Tap_InAir_WithinCoyoteWindow_IsAWallJump_NotADash()
        {
            _fsm.Initialize(_context, PlayerStateId.Fall);
            _context.LastWallSide = -1;
            _context.LastWallExitTime = Time.time;

            _input.TapPending = true;
            _fsm.Tick(0f);

            Assert.AreEqual(PlayerStateId.Jump, _fsm.CurrentId);
        }

        [Test]
        public void WallJump_SpendsCoyote_SoTheNextTapDashes()
        {
            _fsm.Initialize(_context, PlayerStateId.Fall);
            _context.LastWallSide = -1;
            _context.LastWallExitTime = Time.time;

            _input.TapPending = true;
            _fsm.Tick(0f);
            Assert.AreEqual(PlayerStateId.Jump, _fsm.CurrentId);

            _input.TapPending = true;
            _fsm.Tick(0f);

            Assert.AreEqual(PlayerStateId.Dash, _fsm.CurrentId);
        }

        // --- Buffering -------------------------------------------------------------------

        [Test]
        public void BufferedTap_FiresOnLatchEntry()
        {
            _fsm.Initialize(_context, PlayerStateId.Fall);

            // Tap made just before wall contact.
            _input.TapPending = true;
            _fsm.ChangeState(PlayerStateId.Latch);
            _fsm.Tick(0f);

            Assert.AreEqual(PlayerStateId.Jump, _fsm.CurrentId);
            Assert.AreEqual(1, _input.ConsumeCount);
        }

        [Test]
        public void Latch_RefillsDashCharges()
        {
            _context.DashChargesRemaining = 0;

            _fsm.Initialize(_context, PlayerStateId.Latch);

            Assert.AreEqual(_config.DashChargesPerAirtime, _context.DashChargesRemaining);
        }

        [Test]
        public void Latch_FallsThroughToSlide_AfterGraceDuration()
        {
            SenseWall(1);
            _fsm.Initialize(_context, PlayerStateId.Latch);

            _fsm.Tick(_config.LatchGraceDuration + 0.01f);

            Assert.AreEqual(PlayerStateId.Slide, _fsm.CurrentId);
        }

        [Test]
        public void Latch_DropsToFall_WhenTheWallRunsOut()
        {
            SenseWall(1);
            _fsm.Initialize(_context, PlayerStateId.Latch);

            _sensor.Current = PlayerSense.Nothing;
            _fsm.Tick(0.01f);

            Assert.AreEqual(PlayerStateId.Fall, _fsm.CurrentId);
        }

        [Test]
        public void WallJump_LaunchesAwayFromTheWall()
        {
            SenseWall(1); // wall on the right
            _fsm.Initialize(_context, PlayerStateId.Latch);

            _input.TapPending = true;
            _fsm.Tick(0f);

            Assert.AreEqual(PlayerStateId.Jump, _fsm.CurrentId);
            Assert.AreEqual(-_config.JumpHorizontalSpeed, _context.Motor.Velocity.x, 0.001f,
                "Must launch left, away from a wall on the right.");
            Assert.AreEqual(_config.JumpVelocity, _context.Motor.Velocity.y, 0.001f);
            Assert.AreEqual(-1, _context.FacingDirection);
        }

        [Test]
        public void Jump_HandsOverToFall_AtApex()
        {
            _fsm.Initialize(_context, PlayerStateId.Jump);
            _context.Motor.Velocity = new Vector2(5f, 3f);

            _fsm.Tick(0f);
            Assert.AreEqual(PlayerStateId.Jump, _fsm.CurrentId, "Still rising.");

            _context.Motor.Velocity = new Vector2(5f, -0.1f);
            _fsm.Tick(0f);

            Assert.AreEqual(PlayerStateId.Fall, _fsm.CurrentId);
        }

        [Test]
        public void Dash_EndsAfterItsDuration()
        {
            _fsm.Initialize(_context, PlayerStateId.Dash);

            _fsm.Tick(_config.DashDuration + 0.01f);

            Assert.AreEqual(PlayerStateId.Fall, _fsm.CurrentId);
        }

        [Test]
        public void Dash_CutsShort_OnHittingTheFarWall()
        {
            _context.FacingDirection = -1;
            _fsm.Initialize(_context, PlayerStateId.Dash);

            SenseWall(-1);
            _fsm.Tick(0f);

            Assert.AreEqual(PlayerStateId.Latch, _fsm.CurrentId);
        }

        [Test]
        public void Fall_LatchesOnAnyWall()
        {
            _fsm.Initialize(_context, PlayerStateId.Fall);

            SenseWall(-1);
            _fsm.Tick(0f);

            Assert.AreEqual(PlayerStateId.Latch, _fsm.CurrentId);
            Assert.AreEqual(-1, _context.LastWallSide);
        }

        // --- Transition mechanics --------------------------------------------------------

        [Test]
        public void ChangeState_ExitsOldBeforeEnteringNew_ExactlyOnceEach()
        {
            var log = new List<string>();
            var latch = new SpyState(PlayerStateId.Latch, log, _fsm);
            var jump = new SpyState(PlayerStateId.Jump, log, _fsm);
            _fsm.Register(latch);
            _fsm.Register(jump);

            _fsm.Initialize(_context, PlayerStateId.Latch);
            log.Clear();

            _fsm.ChangeState(PlayerStateId.Jump);

            CollectionAssert.AreEqual(new[] { "Latch:Exit", "Jump:Enter" }, log);
            Assert.AreEqual(1, latch.ExitCount);
            Assert.AreEqual(1, jump.EnterCount);
        }

        [Test]
        public void ChangeState_FromInsideEnter_DoesNotRecurse()
        {
            var log = new List<string>();
            var latch = new SpyState(PlayerStateId.Latch, log, _fsm) { RedirectTo = PlayerStateId.Fall };
            var fall = new SpyState(PlayerStateId.Fall, log, _fsm);
            _fsm.Register(latch);
            _fsm.Register(fall);

            _fsm.Initialize(_context, PlayerStateId.Latch);

            Assert.AreEqual(PlayerStateId.Fall, _fsm.CurrentId);
            Assert.AreEqual(1, latch.EnterCount);
            Assert.AreEqual(1, latch.ExitCount);
            Assert.AreEqual(1, fall.EnterCount);
        }

        [Test]
        public void PreviousId_TracksTheStateJustLeft()
        {
            _fsm.Initialize(_context, PlayerStateId.Fall);
            _fsm.ChangeState(PlayerStateId.Latch);

            Assert.AreEqual(PlayerStateId.Fall, _fsm.PreviousId);
            Assert.AreEqual(PlayerStateId.Latch, _fsm.CurrentId);
        }

        // --- Doubles ---------------------------------------------------------------------

        private sealed class FakePlayerInput : IPlayerInput
        {
            public bool Enabled { get; set; } = true;
            public bool TapPending;
            public int ConsumeCount;

            public bool HasBufferedTap(float bufferDuration) => Enabled && TapPending;

            public void ConsumeTap()
            {
                TapPending = false;
                ConsumeCount++;
            }

            public void ClearTap() => TapPending = false;
        }

        private sealed class SpyState : IPlayerState
        {
            private readonly List<string> _log;
            private readonly PlayerStateMachine _fsm;

            public SpyState(PlayerStateId id, List<string> log, PlayerStateMachine fsm)
            {
                Id = id;
                _log = log;
                _fsm = fsm;
            }

            public PlayerStateId Id { get; }
            public int EnterCount;
            public int ExitCount;

            /// <summary>When set, Enter immediately requests another transition.</summary>
            public PlayerStateId? RedirectTo;

            public void Enter()
            {
                EnterCount++;
                _log.Add($"{Id}:Enter");

                if (RedirectTo.HasValue)
                {
                    PlayerStateId target = RedirectTo.Value;
                    RedirectTo = null;
                    _fsm.ChangeState(target);
                }
            }

            public void Exit()
            {
                ExitCount++;
                _log.Add($"{Id}:Exit");
            }

            public void Tick(float deltaTime) { }
            public void FixedTick(float fixedDeltaTime) { }
            public void OnTap() { }
        }
    }
}
