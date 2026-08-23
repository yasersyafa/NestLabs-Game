using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Clinging motionless to a wall. The grace timer is the core of the climb's pressure: stand
    /// still too long and Slide takes over.
    /// </summary>
    public sealed class LatchState : PlayerStateBase
    {
        private float _graceElapsed;

        public LatchState(PlayerContext context) : base(context) { }

        public override PlayerStateId Id => PlayerStateId.Latch;

        public override void Enter()
        {
            _graceElapsed = 0f;

            if (Sense.WallSide != 0)
            {
                Ctx.LastWallSide = Sense.WallSide;
            }

            Ctx.DashChargesRemaining = Config.DashChargesPerAirtime;

            if (Ctx.LastWallSide != 0)
            {
                Ctx.FacingDirection = -Ctx.LastWallSide;
                Ctx.Visual.SetFacing(Ctx.FacingDirection);
            }

            Motor.SetGravityScale(0f);
            Motor.Velocity = Vector2.zero;

            Ctx.Events.Latched(Ctx.LastWallSide);
        }

        public override void Exit()
        {
            Motor.SetGravityScale(1f);
            Ctx.LastWallExitTime = Time.time;
        }

        public override void Tick(float deltaTime)
        {
            // Ran off the top or bottom edge of the wall.
            if (!Sense.OnWall)
            {
                ChangeTo(PlayerStateId.Fall);
                return;
            }

            _graceElapsed += deltaTime;

            if (_graceElapsed >= Config.LatchGraceDuration)
            {
                ChangeTo(PlayerStateId.Slide);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            // Hold a little pressure into the wall so the sensor keeps reporting contact. The motor
            // strips the component pointing into the surface, so this costs no actual movement.
            Motor.Velocity = new Vector2(Ctx.LastWallSide * Config.LatchStickForce, 0f);
            Motor.Move(fixedDeltaTime);
        }

        public override void OnTap()
        {
            ConsumeTap();
            PerformWallJump(Ctx.LastWallSide);
        }
    }
}
