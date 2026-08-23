using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>Terminal. Input is detached; only an external respawn leaves this state.</summary>
    public sealed class DeadState : PlayerStateBase
    {
        public DeadState(PlayerContext context) : base(context) { }

        public override PlayerStateId Id => PlayerStateId.Dead;

        public override void Enter()
        {
            Ctx.Input.Enabled = false;
            Ctx.Input.ClearTap();

            Motor.SetGravityScale(0f);
            Motor.Velocity = Vector2.zero;

            Ctx.Events.Died(Ctx.Transform.position);
        }

        public override void Exit()
        {
            Ctx.Input.Enabled = true;
        }
    }
}
