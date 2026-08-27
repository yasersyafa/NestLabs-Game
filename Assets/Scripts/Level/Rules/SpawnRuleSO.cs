using UnityEngine;

namespace Nestlabs.Level.Rules
{
    public abstract class SpawnRuleSO : ScriptableObject, ISpawnRule
    {
        public abstract void Initialize(SpawnRuleContext ctx);

        // No-op by default: only rules with a pre-run layout (distance burst, wall fill) override this.
        public virtual void Prime(SpawnRuleContext ctx) { }

        public abstract void Tick(SpawnRuleContext ctx, float deltaTime);
    }
}
