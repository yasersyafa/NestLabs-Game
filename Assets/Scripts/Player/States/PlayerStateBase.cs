using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Shared plumbing for every state: the context reference plus shorthand accessors, so concrete
    /// states read as gameplay rather than as pointer chasing. All hooks are virtual no-ops — a
    /// state overrides only what it actually uses.
    /// </summary>
    public abstract class PlayerStateBase : IPlayerState
    {
        protected readonly PlayerContext Ctx;

        protected PlayerStateBase(PlayerContext context)
        {
            Ctx = context;
        }

        public abstract PlayerStateId Id { get; }

        protected PlayerConfigSO Config => Ctx.Config;
        protected PlayerMotor Motor => Ctx.Motor;
        protected PlayerSense Sense => Ctx.Sense;

        public virtual void Enter() { }

        public virtual void Exit() { }

        public virtual void Tick(float deltaTime) { }

        public virtual void FixedTick(float fixedDeltaTime) { }

        public virtual void OnTap() { }

        protected void ChangeTo(PlayerStateId next)
        {
            Ctx.Fsm.ChangeState(next);
        }

        /// <summary>Marks the pending tap as spent. Call this whenever the state acts on a tap.</summary>
        protected void ConsumeTap()
        {
            Ctx.Input.ConsumeTap();
        }

        /// <summary>The airborne default: integrate gravity, then sweep. Used by Jump, Fall and Hit.</summary>
        protected void MoveWithGravity(float fixedDeltaTime)
        {
            Motor.ApplyGravity(Config.Gravity, Config.MaxFallSpeed, fixedDeltaTime);
            Motor.Move(fixedDeltaTime);
        }

        /// <summary>
        /// Launch off <paramref name="wallSide"/>. Shared by Latch, Slide and the coyote-time path
        /// in the airborne states, so the arc is identical no matter which one triggered it.
        /// </summary>
        protected void PerformWallJump(int wallSide)
        {
            int away = wallSide == 0 ? Ctx.FacingDirection : -wallSide;

            Motor.SetGravityScale(1f);
            Motor.Velocity = new Vector2(away * Config.JumpHorizontalSpeed, Config.JumpVelocity);

            Ctx.FacingDirection = away;
            Ctx.Visual.SetFacing(away);
            Ctx.Visual.PlaySquash();
            Ctx.Events.Jumped(wallSide);

            ChangeTo(PlayerStateId.Jump);

            // Coyote time is single-use and must be spent here. Latch/Slide stamp LastWallExitTime
            // on their way out, so without this the very next tap would read as another wall jump
            // instead of the dash the player expects.
            Ctx.LastWallExitTime = float.NegativeInfinity;
        }
    }
}
