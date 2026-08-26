using UnityEngine;

namespace Nestlabs.Level.Rules
{
    public abstract class SpawnRuleSO : ScriptableObject, ISpawnRule
    {
        public abstract void Initialize(SpawnRuleContext ctx);
        public abstract void Tick(SpawnRuleContext ctx, float deltaTime);
    }
}
