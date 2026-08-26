using System;
using Nestlabs.Level;
using Nestlabs.Level.Rules;
using UnityEngine;
using VContainer.Unity;

namespace Nestlabs.Obstacle.Rules
{
    // Ports ObstacleSpawner's behavior: Idle/Moving/Swing share one Y-progression (inherited
    // from DistanceSpawnRuleSO) and one weighted roll picks which kind fires each time. Kept as
    // a single rule rather than three independent ones so the density/mix matches the old
    // spawner exactly - three separate progressions would drift from that feel.
    [CreateAssetMenu(fileName = "WeightedGroupSpawnRule", menuName = "NestLabs/Obstacle/Rules/Weighted Group Spawn Rule")]
    public sealed class WeightedGroupSpawnRuleSO : DistanceSpawnRuleSO
    {
        private enum Kind { Idle, Moving, Swing }

        [Serializable]
        private struct Entry
        {
            public Kind Kind;
            [Min(0f)] public float Weight;
            public IdleObstacle IdlePrefab;
            public MovingObstacle MovingPrefab;
            public SwingObstacle SwingPrefab;
        }

        [Header("Type Selection")]
        [Tooltip("Relative chance per entry. An entry with Weight 0 never spawns.")]
        [SerializeField]
        private Entry[] entries =
        {
            new Entry { Kind = Kind.Idle, Weight = 1f },
            new Entry { Kind = Kind.Moving, Weight = 1f },
            new Entry { Kind = Kind.Swing, Weight = 1f },
        };

        [Header("Spawn Position")]
        [Tooltip("Extra margin kept inside the screen edge (world units) to account for obstacle sprite size.")]
        [SerializeField] private float xEdgeMargin = 0.5f;

        protected override void OnSpawn(float spawnY, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (!TryPickEntry(out Entry entry)) return;

            switch (entry.Kind)
            {
                case Kind.Idle: SpawnIdle(entry, spawnY, ctx, register); break;
                case Kind.Moving: SpawnMoving(entry, spawnY, ctx, register); break;
                case Kind.Swing: SpawnSwing(entry, spawnY, ctx, register); break;
            }
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

        private float GetHalfWidth(SpawnRuleContext ctx) => Mathf.Max(0f, ctx.RawScreenHalfWidth - xEdgeMargin);

        private void SpawnIdle(Entry entry, float spawnY, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (entry.IdlePrefab == null) return;

            float halfWidth = GetHalfWidth(ctx);
            float x = UnityEngine.Random.Range(-halfWidth, halfWidth);
            var instance = ctx.Spawn(entry.IdlePrefab, new Vector3(x, spawnY, 0f), Quaternion.identity);
            (instance as IPoolable)?.OnSpawned(null);
            register(instance.transform);
        }

        private void SpawnMoving(Entry entry, float spawnY, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (entry.MovingPrefab == null) return;

            float halfWidth = GetHalfWidth(ctx);
            float startX = UnityEngine.Random.Range(-halfWidth, halfWidth);
            float endX = UnityEngine.Random.Range(-halfWidth, halfWidth);

            var startPos = new Vector3(startX, spawnY, 0f);
            var endPos = new Vector3(endX, spawnY, 0f);

            var instance = ctx.Spawn(entry.MovingPrefab, startPos, Quaternion.identity);
            instance.Configure(startPos, endPos);
            (instance as IPoolable)?.OnSpawned(null);
            register(instance.transform);
        }

        private void SpawnSwing(Entry entry, float spawnY, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (entry.SwingPrefab == null) return;

            // The ball travels ropeLength*sin(maxAngle) sideways from the anchor, so keep the
            // anchor that much further from the edge or the swing clips off-screen.
            float sideReach = entry.SwingPrefab.RopeLength * Mathf.Sin(entry.SwingPrefab.MaxAngle * Mathf.Deg2Rad);
            float halfWidth = Mathf.Max(0f, GetHalfWidth(ctx) - sideReach);
            float anchorX = UnityEngine.Random.Range(-halfWidth, halfWidth);
            float anchorY = spawnY + entry.SwingPrefab.RopeLength;

            var anchorPos = new Vector3(anchorX, anchorY, 0f);
            var instance = ctx.Spawn(entry.SwingPrefab, anchorPos, Quaternion.identity);
            instance.Configure(anchorPos);
            (instance as IPoolable)?.OnSpawned(null);
            register(instance.transform);
        }
    }
}
