using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Timed horizontal burst with gravity suspended. Taps are swallowed for the whole window —
    /// but not consumed, so a tap made mid-dash fires the instant the dash ends.
    /// </summary>
    public sealed class DashState : PlayerStateBase
    {
        private float _elapsed;
        private int _direction;

        public DashState(PlayerContext context) : base(context) { }

        public override PlayerStateId Id => PlayerStateId.Dash;

        public override void Enter()
        {
            _elapsed = 0f;
            _direction = Ctx.FacingDirection == 0 ? 1 : Ctx.FacingDirection;

            Ctx.DashChargesRemaining--;
            Ctx.LastDashTime = Time.time;

            Motor.SetGravityScale(0f);
            Motor.Velocity = new Vector2(_direction * Config.DashSpeed, 0f);

            Ctx.Visual.SetFacing(_direction);
            Ctx.Events.Dashed(_direction);
        }

        public override void Exit()
        {
            Motor.SetGravityScale(1f);
        }

        public override void Tick(float deltaTime)
        {
            // Slamming into the far wall cuts the dash short — that is the point of dashing.
            if (Sense.OnWall && Sense.WallSide == _direction)
            {
                ChangeTo(PlayerStateId.Latch);
                return;
            }

            _elapsed += deltaTime;

            if (_elapsed >= Config.DashDuration)
            {
                ChangeTo(PlayerStateId.Fall);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            // Held rather than integrated: a dash is a constant-speed burst, no gravity, no drag.
            Motor.Velocity = new Vector2(_direction * Config.DashSpeed, 0f);
            Motor.Move(fixedDeltaTime);
        }
    }
}
