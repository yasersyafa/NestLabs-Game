using NestLabs.Node;
using UnityEngine;

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
            BleedOffLaunchSpeed(fixedDeltaTime);
            MoveWithGravity(fixedDeltaTime);
        }

        /// <summary>
        /// Eases horizontal speed from a grapple launch back down to normal air speed. Y is left to
        /// gravity, which already handles a near-vertical launch on its own.
        /// </summary>
        private void BleedOffLaunchSpeed(float fixedDeltaTime)
        {
            if (Ctx.GrappleDecayRemaining <= 0f)
            {
                return;
            }

            Ctx.GrappleDecayRemaining -= fixedDeltaTime;

            Vector2 v = Motor.Velocity;
            float target = Mathf.Sign(v.x) * Config.GrappleExitSpeed;

            if (Mathf.Abs(v.x) <= Config.GrappleExitSpeed)
            {
                Ctx.GrappleDecayRemaining = 0f;
                return;
            }

            v.x = Mathf.MoveTowards(v.x, target, Ctx.GrappleDecayRate * fixedDeltaTime);
            Motor.Velocity = v;
        }

        public override void OnTap()
        {
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
