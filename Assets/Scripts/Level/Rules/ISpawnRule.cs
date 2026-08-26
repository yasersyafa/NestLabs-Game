namespace Nestlabs.Level.Rules
{
    // Common tick contract every spawn rule implements, regardless of timing model
    // (distance-based, interval-based, or anything added later). LevelGenerator only
    // ever calls these two methods - it never branches on rule type.
    public interface ISpawnRule
    {
        void Initialize(SpawnRuleContext ctx);
        void Tick(SpawnRuleContext ctx, float deltaTime);
    }
}
