using NestLabs.Node;
using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// The grapple pull, in two phases. First a short wind-up where the player hangs still, faces
    /// the node and the world drops into slow motion. Then a straight-line launch at the node's own
    /// force, gravity suspended, ending the moment the player crosses the node. Velocity is kept on
    /// exit and bled off by Fall, so the pull settles into normal air speed rather than stopping.
    /// Taps are swallowed for the whole window but not consumed, so a tap made mid-pull fires the
    /// instant the pull ends.
    /// </summary>
    public sealed class DashState : PlayerStateBase
    {
        // Below this the direction is noise, not aim.
        private const float MinLaunchDistanceSqr = 0.0001f;

        private Vector2 _direction;
        private float _speed;
        private float _elapsed;
        private NodeBase _node;

        private bool _launched;
        private float _anticipationEndsAt;

        public DashState(PlayerContext context) : base(context) { }

        public override PlayerStateId Id => PlayerStateId.Dash;

        public override void Enter()
        {
            _elapsed = 0f;
            _launched = false;
            _node = Ctx.ActiveNode;

            if (_node == null)
            {
                ChangeTo(PlayerStateId.Fall);
                return;
            }

            Vector2 toNode = _node.Position - (Vector2)Ctx.Transform.position;

            // Sitting exactly on the node leaves no direction to launch along.
            if (toNode.sqrMagnitude < MinLaunchDistanceSqr)
            {
                ChangeTo(PlayerStateId.Fall);
                return;
            }

            _direction = toNode.normalized;
            _speed = _node.LaunchForce;
            _node.Consume();

            // Hang still for the wind-up. Unscaled, or the slow-mo would stretch it by its own factor.
            Motor.SetGravityScale(0f);
            Motor.Velocity = Vector2.zero;
            _anticipationEndsAt = Time.unscaledTime + Config.GrappleAnticipationDuration;

            // Turning to face the node during the wind-up is most of what sells the launch.
            int facing = _direction.x < 0f ? -1 : 1;
            Ctx.FacingDirection = facing;
            Ctx.Visual.SetFacing(facing);

            // The dip lasts exactly as long as the wind-up, so it lifts the instant the launch fires.
            Ctx.Hitstop.Begin(Config.GrappleTimeScale, Config.GrappleAnticipationDuration);
            Ctx.Visual.PlayGrappleAnticipation(_direction, Config.GrappleAnticipationDuration);

            // Fires here so a zero anticipation duration still launches on this frame.
            TryLaunch();
        }

        public override void Exit()
        {
            Motor.SetGravityScale(1f);

            if (_launched)
            {
                Ctx.ArmGrappleDecay(_speed);
            }

            if (Ctx.Trail != null) Ctx.Trail.End();

            Ctx.ActiveNode = null;
            _node = null;
        }

        public override void Tick(float deltaTime)
        {
            if (_node == null)
            {
                ChangeTo(PlayerStateId.Fall);
                return;
            }

            // Every test below assumes the player is moving, so none of them apply mid-wind-up.
            if (!TryLaunch())
            {
                return;
            }

            // Crossed the node, so the pull is done. Velocity is deliberately left alone: nothing
            // in Exit or Fall.Enter touches it, which is what carries the launch speed onward.
            if (Vector2.Dot(_node.Position - (Vector2)Ctx.Transform.position, _direction) <= 0f)
            {
                ChangeTo(PlayerStateId.Fall);
                return;
            }

            // Slamming into the far wall cuts the pull short, same as the old dash.
            if (Sense.OnWall && Sense.WallSide == (_direction.x < 0f ? -1 : 1))
            {
                ChangeTo(PlayerStateId.Latch);
                return;
            }

            _elapsed += deltaTime;

            // Safety valve only. Geometry between the player and the node means the crossing test
            // never passes, and without this the player hangs with gravity off.
            if (_elapsed >= Config.GrappleMaxDuration)
            {
                ChangeTo(PlayerStateId.Fall);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            // Held rather than integrated: the pull is a constant-speed line, no gravity, no drag.
            Motor.Velocity = _launched ? _direction * _speed : Vector2.zero;
            Motor.Move(fixedDeltaTime);
        }

        /// <summary>Releases the launch once the wind-up is spent. True once the player is moving.</summary>
        private bool TryLaunch()
        {
            if (_launched) return true;
            if (Time.unscaledTime < _anticipationEndsAt) return false;

            _launched = true;
            Motor.Velocity = _direction * _speed;

            if (Ctx.Trail != null) Ctx.Trail.Begin();
            Ctx.Events.Dashed(_direction.x < 0f ? -1 : 1);
            return true;
        }
    }
}
