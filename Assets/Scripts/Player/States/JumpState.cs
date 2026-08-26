using NestLabs.Node;

namespace NestLabs.Player
{
    /// <summary>Ascending arc after leaving a wall. Hands over to Fall at the apex.</summary>
    public sealed class JumpState : PlayerStateBase
    {
        public JumpState(PlayerContext context) : base(context) { }

        public override PlayerStateId Id => PlayerStateId.Jump;

        public override void Enter()
        {
            Motor.SetGravityScale(1f);
        }

        public override void Tick(float deltaTime)
        {
            // Only the far wall counts. Latching onto the wall we just left would fire instantly,
            // because it is still inside the sensor's probe distance for a frame or two.
            if (Sense.OnWall && Sense.WallSide != Ctx.LastWallSide)
            {
                ChangeTo(PlayerStateId.Latch);
                return;
            }

            if (Motor.Velocity.y <= 0f)
            {
                ChangeTo(PlayerStateId.Fall);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            MoveWithGravity(fixedDeltaTime);
        }

        public override void OnTap()
        {
            // Coyote first: a tap a hair too late must still read as the wall jump the player meant.
            if (Ctx.HasCoyoteWall)
            {
                ConsumeTap();
                PerformWallJump(Ctx.LastWallSide);
                return;
            }

            if (!Ctx.TryGetNodeInRange(out NodeBase node))
            {
                // Deliberately leave the tap buffered, so it fires the moment a node comes in range.
                return;
            }

            ConsumeTap();
            Ctx.ActiveNode = node;
            ChangeTo(PlayerStateId.Dash);
        }
    }
}
