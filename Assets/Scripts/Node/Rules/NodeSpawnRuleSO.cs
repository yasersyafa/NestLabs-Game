using System;
using Nestlabs.Level;
using Nestlabs.Level.Rules;
using UnityEngine;
using VContainer.Unity;

namespace NestLabs.Node.Rules
{
    // Same weighted-variant-pick shape as Nestlabs.Obstacle.Rules.WeightedGroupSpawnRuleSO, just
    // for grapple points instead of hazards: one shared Y-progression (inherited from
    // DistanceSpawnRuleSO), a weighted roll over prefab variants. Kept independent of the obstacle
    // group's progression - Node is a traversal tool, not a hazard, so its cadence is tuned on its
    // own. Variants (e.g. Node_Strong) are just additional entries - a pure data change, no code.
    [CreateAssetMenu(fileName = "NodeSpawnRule", menuName = "NestLabs/Node/Rules/Node Spawn Rule")]
    public sealed class NodeSpawnRuleSO : DistanceSpawnRuleSO
    {
        [Serializable]
        private struct Entry
        {
            public NodeBase Prefab;
            [Min(0f)] public float Weight;
        }

        [Header("Variants")]
        [Tooltip("Relative chance per variant. A variant with Weight 0 never spawns.")]
        [SerializeField] private Entry[] entries;

        [Header("Spawn Position")]
        [Tooltip("Extra margin kept inside the screen edge (world units), so nodes stay inside the wall corridor.")]
        [SerializeField] private float xEdgeMargin = 1f;

        protected override void OnSpawn(float spawnY, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (!TryPickEntry(out Entry entry) || entry.Prefab == null) return;

            float halfWidth = Mathf.Max(0f, ctx.RawScreenHalfWidth - xEdgeMargin);
            float x = UnityEngine.Random.Range(-halfWidth, halfWidth);

            var instance = ctx.Spawn(entry.Prefab, new Vector3(x, spawnY, 0f), Quaternion.identity);
            (instance as IPoolable)?.OnSpawned(null);
            register(instance.transform);

            // Reserve the grab radius so the obstacle rule keeps hazards out of it.
            ctx.AddClaim(instance.transform, entry.Prefab.ClaimRadius);
        }

        private bool TryPickEntry(out Entry picked)
        {
            float total = 0f;
            foreach (Entry e in entries) total += Mathf.Max(0f, e.Weight);

            if (total <= 0f)
            {
                picked = default;
                return false;
            }

            float roll = UnityEngine.Random.Range(0f, total);
            float cursor = 0f;
            foreach (Entry e in entries)
            {
                cursor += Mathf.Max(0f, e.Weight);
                if (roll <= cursor)
                {
                    picked = e;
                    return true;
                }
            }

            picked = entries[entries.Length - 1];
            return true;
        }
    }
}
