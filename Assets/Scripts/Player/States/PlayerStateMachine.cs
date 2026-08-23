using System;
using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Owns the active state and the transitions between them. Fully implemented — the feature
    /// bodies inside each state are stubs, but this plumbing is real so the skeleton is testable.
    /// </summary>
    public sealed class PlayerStateMachine
    {
        // Array rather than Dictionary: PlayerStateId is a small dense enum, so an indexed lookup
        // is both faster and free of any comparer allocation.
        private static readonly int StateCount = Enum.GetValues(typeof(PlayerStateId)).Length;

        private readonly IPlayerState[] _states = new IPlayerState[StateCount];

        private PlayerContext _context;
        private bool _isChanging;
        private bool _hasPending;
        private PlayerStateId _pending;

        public IPlayerState Current { get; private set; }

        public PlayerStateId CurrentId => Current?.Id ?? PlayerStateId.None;

        public PlayerStateId PreviousId { get; private set; } = PlayerStateId.None;

        /// <summary>Fired after the new state is installed but before its Enter runs.</summary>
        public event Action<PlayerStateId, PlayerStateId> StateChanged;

        public void Register(IPlayerState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _states[(int)state.Id] = state;
        }

        /// <summary>Binds the context and enters the first state. Register all states before calling.</summary>
        public void Initialize(PlayerContext context, PlayerStateId initial)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Current = null;
            PreviousId = PlayerStateId.None;
            ChangeState(initial);
        }

        /// <summary>
        /// Exits the current state and enters <paramref name="next"/>. Safe to call from inside
        /// Enter or Exit: the request is queued and drained by the outermost call rather than
        /// recursing.
        /// </summary>
        public void ChangeState(PlayerStateId next)
        {
            _pending = next;
            _hasPending = true;

            if (_isChanging)
            {
                return;
            }

            _isChanging = true;
            try
            {
                while (_hasPending)
                {
                    PlayerStateId target = _pending;
                    _hasPending = false;

                    IPlayerState state = _states[(int)target];
                    if (state == null)
                    {
                        Debug.LogError($"[PlayerStateMachine] State '{target}' was never registered.");
                        return;
                    }

                    PlayerStateId from = CurrentId;

                    Current?.Exit();
                    Current = state;
                    PreviousId = from;

                    // Visuals are driven directly rather than through the message bus: the player's
                    // own animation must not depend on a container being wired up.
                    _context.Visual.PlayForState(target);
                    _context.Events.StateChanged(from, target);
                    StateChanged?.Invoke(from, target);

                    state.Enter();
                }
            }
            finally
            {
                _isChanging = false;
            }
        }

        public void Tick(float deltaTime)
        {
            if (Current == null)
            {
                return;
            }

            // A buffered tap keeps being offered until some state consumes it. That is what makes a
            // tap fired mid-dash fire again the moment the dash ends.
            if (_context.Input.HasBufferedTap(_context.Config.InputBufferDuration))
            {
                Current.OnTap();
            }

            Current.Tick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            Current?.FixedTick(fixedDeltaTime);
        }
    }
}
