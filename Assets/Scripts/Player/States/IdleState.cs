using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// The pre-run pose. The player stands still on the starting floor with gravity off; the world
    /// is frozen (flow state is Menu, so LevelGenerator and ScoreService are idle). The first tap
    /// starts the run and launches the player off toward the first wall.
    /// </summary>
    public sealed class IdleState : PlayerStateBase
    {
        public IdleState(PlayerContext context) : base(context) { }

        public override PlayerStateId Id => PlayerStateId.Idle;

        public override void Enter()
        {
            Motor.SetGravityScale(0f);
            Motor.Velocity = Vector2.zero;
        }

        public override void OnTap()
        {
            ConsumeTap();

            // Flips the flow state to Play, which is what wakes LevelGenerator's spawning and
            // ScoreService's run tracking. Everything else keys off that.
            Ctx.GameState.EnterPlay();

            // The player spawns on the right side of the shaft, so the opening move is always a
            // jump off the right wall toward the left one. Stamping LastWallSide keeps the launch
            // arc from re-latching the wall it just left.
            Ctx.LastWallSide = 1;
            PerformWallJump(1);
        }
    }
}
