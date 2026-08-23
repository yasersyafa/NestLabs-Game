namespace NestLabs.Player
{
    /// <summary>Descending through the air, after an apex or after a dash ends.</summary>
    public sealed class FallState : PlayerStateBase
    {
        public FallState(PlayerContext context) : base(context) { }

        public override PlayerStateId Id => PlayerStateId.Fall;

        public override void Enter()
        {
            Motor.SetGravityScale(1f);
        }

        public override void Tick(float deltaTime)
        {
            // Unlike Jump, any wall will do here — sliding back down the wall you left is legal.
            if (Sense.OnWall)
            {
                ChangeTo(PlayerStateId.Latch);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            MoveWithGravity(fixedDeltaTime);
        }

        public override void OnTap()
        {
            if (Ctx.HasCoyoteWall)
            {
                ConsumeTap();
                PerformWallJump(Ctx.LastWallSide);
                return;
            }

            if (!Ctx.CanDash)
            {
                return;
            }

            ConsumeTap();
            ChangeTo(PlayerStateId.Dash);
        }
    }
}
