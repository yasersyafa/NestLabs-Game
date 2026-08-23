namespace NestLabs.Player
{
    /// <summary>
    /// Knockback plus control lock. Entered only after <see cref="PlayerHealth.TryApplyDamage"/>
    /// has already accepted the hit, so this state never re-checks i-frames.
    /// </summary>
    public sealed class HitState : PlayerStateBase
    {
        private float _elapsed;

        public HitState(PlayerContext context) : base(context) { }

        public override PlayerStateId Id => PlayerStateId.Hit;

        public override void Enter()
        {
            _elapsed = 0f;

            Motor.SetGravityScale(1f);
            Motor.Velocity = Ctx.Health.GetKnockback();

            Ctx.Visual.PlayHitFlash();

            if (Ctx.Health.IsDead)
            {
                ChangeTo(PlayerStateId.Dead);
            }
        }

        public override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            if (_elapsed < Config.HitStunDuration)
            {
                return;
            }

            // Recovering onto a wall feels better than being dropped into a fall next to one.
            ChangeTo(Sense.OnWall ? PlayerStateId.Latch : PlayerStateId.Fall);
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            MoveWithGravity(fixedDeltaTime);
        }
    }
}
