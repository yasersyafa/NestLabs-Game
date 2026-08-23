using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Descending the wall. Accelerates from SlideSpeed toward SlideMaxSpeed, so hesitating costs
    /// more the longer it lasts.
    /// </summary>
    public sealed class SlideState : PlayerStateBase
    {
        public SlideState(PlayerContext context) : base(context) { }

        public override PlayerStateId Id => PlayerStateId.Slide;

        public override void Enter()
        {
            if (Sense.WallSide != 0)
            {
                Ctx.LastWallSide = Sense.WallSide;
            }

            Motor.SetGravityScale(0f);
            Motor.Velocity = new Vector2(0f, -Config.SlideSpeed);
        }

        public override void Exit()
        {
            Motor.SetGravityScale(1f);
            Ctx.LastWallExitTime = Time.time;
        }

        public override void Tick(float deltaTime)
        {
            if (!Sense.OnWall)
            {
                ChangeTo(PlayerStateId.Fall);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Vector2 v = Motor.Velocity;

            v.y -= Config.SlideAcceleration * fixedDeltaTime;
            if (v.y < -Config.SlideMaxSpeed)
            {
                v.y = -Config.SlideMaxSpeed;
            }

            // Keep leaning into the wall, same as Latch.
            v.x = Ctx.LastWallSide * Config.LatchStickForce;

            Motor.Velocity = v;
            Motor.Move(fixedDeltaTime);
        }

        public override void OnTap()
        {
            ConsumeTap();
            PerformWallJump(Ctx.LastWallSide);
        }
    }
}
