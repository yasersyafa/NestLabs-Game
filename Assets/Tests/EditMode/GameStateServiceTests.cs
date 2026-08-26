using System;
using MessagePipe;
using NestLabs.Player;
using NestLabs.Shared.Flow;
using NUnit.Framework;
using UnityEngine;

namespace NestLabs.Tests
{
    /// <summary>
    /// Locks down the flow state machine: the allowed transition table, who owns the pause, and
    /// that every accepted move reports itself exactly once.
    /// </summary>
    public sealed class GameStateServiceTests
    {
        private FakeSubscriber<PlayerDiedEvent> _died;
        private RecordingPublisher<GameStateChangedEvent> _published;
        private FakeHitstop _hitstop;
        private GameStateService _service;

        [SetUp]
        public void SetUp()
        {
            _died = new FakeSubscriber<PlayerDiedEvent>();
            _published = new RecordingPublisher<GameStateChangedEvent>();
            _hitstop = new FakeHitstop();
            _service = new GameStateService(_died, _published, _hitstop);
        }

        [TearDown]
        public void TearDown() => _service.Dispose();

        private void Die() => _died.Fire(new PlayerDiedEvent(Vector2.zero));

        [Test]
        public void Boots_InPlay()
        {
            Assert.AreEqual(GameState.Play, _service.Current);
            Assert.IsTrue(_service.IsPlaying);
            Assert.AreEqual(0, _published.Messages.Count, "Construction has no From state to report.");
        }

        [Test]
        public void Pause_FreezesTime_AndResumeRestoresIt()
        {
            _service.Pause();

            Assert.AreEqual(GameState.Pause, _service.Current);
            Assert.IsFalse(_service.IsPlaying);
            Assert.IsTrue(_hitstop.Paused, "Pause must go through IHitstop, not Time.timeScale.");

            _service.Resume();

            Assert.AreEqual(GameState.Play, _service.Current);
            Assert.IsFalse(_hitstop.Paused);
        }

        [Test]
        public void Death_ComesFromThePlayerEvent()
        {
            Die();

            Assert.AreEqual(GameState.Death, _service.Current);
            Assert.IsFalse(_service.IsPlaying);
        }

        [Test]
        public void Death_IsTerminal_UntilSomethingLeavesItExplicitly()
        {
            Die();
            _published.Messages.Clear();

            _service.Pause();
            Assert.AreEqual(GameState.Death, _service.Current, "Pause is only legal from Play.");

            Die();
            Assert.AreEqual(GameState.Death, _service.Current, "A second death must not re-report.");
            Assert.AreEqual(0, _published.Messages.Count);

            _service.EnterPlay();
            Assert.AreEqual(GameState.Play, _service.Current);
        }

        [Test]
        public void Menu_IsOnlyReachableFromAStoppedRun()
        {
            _service.EnterMenu();
            Assert.AreEqual(GameState.Play, _service.Current, "A live run must be paused or ended first.");

            _service.Pause();
            _service.EnterMenu();

            Assert.AreEqual(GameState.Menu, _service.Current);
            Assert.IsFalse(_hitstop.Paused, "Backing out of a pause must not leave time frozen.");
        }

        [Test]
        public void EveryAcceptedMove_PublishesItsFromAndTo_Once()
        {
            _service.Pause();
            _service.Resume();
            Die();

            Assert.AreEqual(3, _published.Messages.Count);
            AssertChange(0, GameState.Play, GameState.Pause);
            AssertChange(1, GameState.Pause, GameState.Play);
            AssertChange(2, GameState.Play, GameState.Death);
        }

        [Test]
        public void RejectedMove_PublishesNothing()
        {
            _service.Resume(); // already in Play

            Assert.AreEqual(GameState.Play, _service.Current);
            Assert.AreEqual(0, _published.Messages.Count);
        }

        [Test]
        public void Subscriber_ReadingCurrent_SeesTheNewState()
        {
            GameState seen = GameState.None;
            _published.OnPublish = _ => seen = _service.Current;

            _service.Pause();

            Assert.AreEqual(GameState.Pause, seen);
        }

        [Test]
        public void Dispose_StopsListeningForDeaths()
        {
            _service.Dispose();

            Die();

            Assert.AreEqual(GameState.Play, _service.Current);
        }

        private void AssertChange(int index, GameState from, GameState to)
        {
            Assert.AreEqual(from, _published.Messages[index].From, $"message {index} From");
            Assert.AreEqual(to, _published.Messages[index].To, $"message {index} To");
        }

        // --- Doubles ---------------------------------------------------------------------

        private sealed class FakeHitstop : IHitstop
        {
            public bool Paused;

            public void Begin(float scale, float unscaledDuration) { }
            public void Cancel() { }
            public void SetPaused(bool paused) => Paused = paused;
        }

        private sealed class RecordingPublisher<T> : IPublisher<T>
        {
            public readonly System.Collections.Generic.List<T> Messages = new System.Collections.Generic.List<T>();

            /// <summary>Stands in for a subscriber that reads state back inside its handler.</summary>
            public Action<T> OnPublish;

            public void Publish(T message)
            {
                Messages.Add(message);
                OnPublish?.Invoke(message);
            }
        }

        private sealed class FakeSubscriber<T> : ISubscriber<T>
        {
            private IMessageHandler<T> _handler;

            public IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters)
            {
                _handler = handler;
                return new Unsubscribe(this);
            }

            public void Fire(T message) => _handler?.Handle(message);

            private sealed class Unsubscribe : IDisposable
            {
                private readonly FakeSubscriber<T> _owner;
                public Unsubscribe(FakeSubscriber<T> owner) => _owner = owner;
                public void Dispose() => _owner._handler = null;
            }
        }
    }
}
