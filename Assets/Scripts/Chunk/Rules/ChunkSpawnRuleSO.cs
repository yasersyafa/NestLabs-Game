using System;
using System.Collections.Generic;
using Nestlabs.Level;
using Nestlabs.Level.Rules;
using UnityEngine;

namespace Nestlabs.Chunk.Rules
{
    // Replaces WeightedGroupSpawnRuleSO (obstacles) + NodeSpawnRuleSO (nodes) as two independent
    // Y-progressions reconciled reactively at spawn time via SpawnRuleContext.AddClaim/
    // ClaimClearance - WeightedGroupSpawnRuleSO.PickX documented its own failure mode: when the
    // corridor is narrower than a node's required clearance (routine at 9:16 with a Strong node),
    // it falls back to spawning the obstacle overlapping the node's claim anyway rather than skip.
    //
    // A chunk fires both node and obstacle placement from one hand-authored ChunkSO, so there is no
    // runtime retry/fallback path left to break - positions are solvable by construction. Pool
    // entries are picked by weighted random, biased by a distance-based per-tier ramp, with a
    // short anti-repeat window so the same layout doesn't fire back-to-back.
    [CreateAssetMenu(fileName = "ChunkSpawnRule", menuName = "NestLabs/Chunk/Rules/Chunk Spawn Rule")]
    public sealed class ChunkSpawnRuleSO : DistanceSpawnRuleSO
    {
        [Serializable]
        private struct TierRamp
        {
            public ChunkTier tier;
            [Tooltip("World-Y (climbed distance) below which this tier's chunks never spawn.")]
            public float introduceAtDistance;
            [Tooltip("World-Y at/above which this tier's chunks spawn at full weight. Same as introduceAtDistance for a hard cutoff.")]
            public float fullWeightAtDistance;
        }

        [Header("Chunk Pool")]
        [SerializeField] private ChunkSO[] pool = Array.Empty<ChunkSO>();

        [Header("Difficulty Curve")]
        [Tooltip("A tier with no entry here spawns at full weight from the very start.")]
        [SerializeField] private TierRamp[] tierRamps = Array.Empty<TierRamp>();

        [Header("Anti-Repeat")]
        [Tooltip("How many recently-fired chunk IDs to exclude from the next pick. 0 disables.")]
        [Min(0)] [SerializeField] private int antiRepeatWindow = 3;

        [Header("Spawn Position")]
        [Tooltip("Extra margin kept inside the screen edge (world units).")]
        [SerializeField] private float xEdgeMargin = 0.5f;

        private readonly Queue<string> _recentChunkIds = new();
        private readonly List<int> _candidateIndices = new();

        public override void Initialize(SpawnRuleContext ctx)
        {
            base.Initialize(ctx);
            _recentChunkIds.Clear();
            ValidateFootprints();
        }

        protected override void OnSpawn(float spawnY, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (ctx.Player == null) return;
            if (!TryPickChunk(ctx.Player.position.y, out ChunkSO chunk)) return;

            RememberChunk(chunk.ChunkId);

            float halfWidth = Mathf.Max(0f, ctx.RawScreenHalfWidth - xEdgeMargin);
            // >= 1 on every supported device, since referenceHalfWidth is authored as the
            // narrowest corridor - live positions only ever spread further apart than the
            // authored-safe layout, never compress below it.
            float scale = halfWidth / Mathf.Max(0.01f, chunk.ReferenceHalfWidth);

            foreach (ChunkSO.ChunkEntry entry in chunk.Entries)
            {
                float x = Mathf.Clamp(
                    entry.relativeX * scale + UnityEngine.Random.Range(entry.jitterX.x, entry.jitterX.y),
                    -halfWidth, halfWidth);
                float y = spawnY + entry.relativeY;

                switch (entry.type)
                {
                    case ChunkSO.EntryType.Node: SpawnNode(entry, x, y, ctx, register); break;
                    case ChunkSO.EntryType.IdleObstacle: SpawnIdle(entry, x, y, ctx, register); break;
                    case ChunkSO.EntryType.MovingObstacle: SpawnMoving(entry, x, y, scale, ctx, register); break;
                    // Anchor point, not the bob's rest position - simpler to author by hand than
                    // WeightedGroupSpawnRuleSO's bob-relative placement, and the author is already
                    // responsible for keeping the anchor clear of the edge given its own rope reach.
                    case ChunkSO.EntryType.SwingObstacle: SpawnSwing(entry, x, y, ctx, register); break;
                }
            }
        }

        private bool TryPickChunk(float distanceY, out ChunkSO picked)
        {
            if (TryWeightedPick(distanceY, excludeRecent: true, out picked)) return true;

            // Recency filter starved every candidate (e.g. a tiny pool) - refuse to skip the
            // spawn entirely, same philosophy WeightedGroupSpawnRuleSO's PickX fallback used.
            return TryWeightedPick(distanceY, excludeRecent: false, out picked);
        }

        private bool TryWeightedPick(float distanceY, bool excludeRecent, out ChunkSO picked)
        {
            _candidateIndices.Clear();
            float total = 0f;

            for (int i = 0; i < pool.Length; i++)
            {
                ChunkSO chunk = pool[i];
                if (chunk == null) continue;
                if (excludeRecent && _recentChunkIds.Contains(chunk.ChunkId)) continue;

                float w = EffectiveWeight(chunk, distanceY);
                if (w <= 0f) continue;

                _candidateIndices.Add(i);
                total += w;
            }

            if (total <= 0f)
            {
                picked = null;
                return false;
            }

            float roll = UnityEngine.Random.Range(0f, total);
            float cursor = 0f;
            foreach (int i in _candidateIndices)
            {
                cursor += EffectiveWeight(pool[i], distanceY);
                if (roll <= cursor)
                {
                    picked = pool[i];
                    return true;
                }
            }

            picked = pool[_candidateIndices[_candidateIndices.Count - 1]];
            return true;
        }

        private float EffectiveWeight(ChunkSO chunk, float distanceY)
        {
            if (chunk.Weight <= 0f) return 0f;
            return chunk.Weight * RampMultiplier(chunk.Tier, distanceY);
        }

        private float RampMultiplier(ChunkTier tier, float distanceY)
        {
            foreach (TierRamp ramp in tierRamps)
            {
                if (ramp.tier != tier) continue;

                if (ramp.fullWeightAtDistance <= ramp.introduceAtDistance)
                {
                    return distanceY >= ramp.introduceAtDistance ? 1f : 0f;
                }

                return Mathf.Clamp01(Mathf.InverseLerp(ramp.introduceAtDistance, ramp.fullWeightAtDistance, distanceY));
            }

            // No ramp configured for this tier - always eligible at full weight.
            return 1f;
        }

        private void RememberChunk(string id)
        {
            if (antiRepeatWindow <= 0) return;

            _recentChunkIds.Enqueue(id);
            while (_recentChunkIds.Count > antiRepeatWindow) _recentChunkIds.Dequeue();
        }

        // Catches an authoring mistake that would otherwise only show up as two chunks' Y-ranges
        // overlapping at runtime, far harder to trace back to the offending asset.
        private void ValidateFootprints()
        {
            float gapMin = SpawnYGapMin;
            foreach (ChunkSO chunk in pool)
            {
                if (chunk == null) continue;
                if (chunk.FootprintHeight > gapMin)
                {
                    Debug.LogError($"[ChunkSpawnRuleSO] Chunk '{chunk.ChunkId}' footprintHeight " +
                                    $"({chunk.FootprintHeight:F1}) exceeds this rule's spawnYGapMin " +
                                    $"({gapMin:F1}) - consecutive chunks can overlap in Y.", this);
                }
            }
        }

        private static void SpawnNode(ChunkSO.ChunkEntry entry, float x, float y, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (entry.nodePrefab == null) return;

            var instance = ctx.Spawn(entry.nodePrefab, new Vector3(x, y, 0f), Quaternion.identity);
            (instance as IPoolable)?.OnSpawned(null);
            register(instance.transform);

            // Safety net only - chunk authoring already keeps obstacles clear of this radius, this
            // just protects a future chunk-unaware rule (or overlap at scene Prime) for free.
            ctx.AddClaim(instance.transform, entry.nodePrefab.ClaimRadius);
        }

        private static void SpawnIdle(ChunkSO.ChunkEntry entry, float x, float y, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (entry.idlePrefab == null) return;

            var instance = ctx.Spawn(entry.idlePrefab, new Vector3(x, y, 0f), Quaternion.identity);
            (instance as IPoolable)?.OnSpawned(null);
            register(instance.transform);
        }

        private static void SpawnMoving(ChunkSO.ChunkEntry entry, float x, float y, float scale, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (entry.movingPrefab == null) return;

            float endX = Mathf.Clamp(entry.movingEndRelativeX * scale, -ctx.RawScreenHalfWidth, ctx.RawScreenHalfWidth);
            var startPos = new Vector3(x, y, 0f);
            var endPos = new Vector3(endX, y, 0f);

            var instance = ctx.Spawn(entry.movingPrefab, startPos, Quaternion.identity);
            instance.Configure(startPos, endPos);
            (instance as IPoolable)?.OnSpawned(null);
            register(instance.transform);
        }

        private static void SpawnSwing(ChunkSO.ChunkEntry entry, float x, float y, SpawnRuleContext ctx, Action<Transform> register)
        {
            if (entry.swingPrefab == null) return;

            var anchorPos = new Vector3(x, y, 0f);
            var instance = ctx.Spawn(entry.swingPrefab, anchorPos, Quaternion.identity);
            instance.Configure(anchorPos);
            (instance as IPoolable)?.OnSpawned(null);
            register(instance.transform);
        }
    }
}
