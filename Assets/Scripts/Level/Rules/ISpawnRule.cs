namespace Nestlabs.Level.Rules
{
    // Common tick contract every spawn rule implements, regardless of timing model
    // (distance-based, interval-based, or anything added later). LevelGenerator only
    // ever calls these two methods - it never branches on rule type.
    public interface ISpawnRule
    {
        void Initialize(SpawnRuleContext ctx);

        // Fills the opening layout (initial burst / initial fill) once, so it is on screen during
        // the pre-run ready pose. Called by LevelGenerator before the run starts; idempotent, and
        // rules with no pre-fill (timer-based) leave it a no-op. Steady-state spawning still only
        // happens in Tick while the game is playing.
        void Prime(SpawnRuleContext ctx);

        void Tick(SpawnRuleContext ctx, float deltaTime);
    }
}
