using System.Collections.Generic;
using Nestlabs.Level.Rules;
using UnityEngine;
using VContainer.Unity;

namespace Nestlabs.Wall.Rules
{
    // Wall is "always there" climbable terrain, not a one-at-a-time hazard, so it doesn't fit
    // DistanceSpawnRuleSO's single-fire-per-gap model. Instead this tiles wallPrefab's own
    // Collider2D height edge-to-edge into a continuous mirrored left+right column, pre-filling
    // initialSegmentCount pairs immediately, then extending by refillSegmentCount more pairs
    // whenever the player gets within refillLookahead of the topmost spawned segment.
    [CreateAssetMenu(fileName = "WallPairSpawnRule", menuName = "NestLabs/Wall/Rules/Wall Pair Spawn Rule")]
    public sealed class WallPairSpawnRuleSO : SpawnRuleSO
    {
        [Header("References")]
        [SerializeField] private WallTerrain wallPrefab;

        [Header("Spawn Position")]
        [Tooltip("Extra margin kept inside the screen edge (world units) to account for wall sprite size.")]
        [SerializeField] private float xInset = 0f;

        [Header("Fill")]
        [Tooltip("How many mirrored pairs to spawn immediately when the level starts.")]
        [SerializeField] private int initialSegmentCount = 5;
        [Tooltip("How many more mirrored pairs to add each time the player nears the top of the filled column.")]
        [SerializeField] private int refillSegmentCount = 5;
        [Tooltip("Refill once the player gets this close to the topmost spawned segment.")]
        [SerializeField] private float refillLookahead = 8f;

        [Header("Despawn")]
        [Tooltip("Destroy a segment once the player has climbed this far above it.")]
        [SerializeField] private float cullDistanceBelowPlayer = 12f;

        private float _segmentHeight;
        private float _nextSegmentY;
        private bool _hasFilledInitial;
        private readonly List<Transform> _active = new();

        // Only resets pure state here - deliberately does NOT spawn anything. This runs from
        // LevelGenerator.Awake(), before VContainer's [Inject] Construct() is guaranteed to have
        // set ctx.Resolver (see LevelGenerator.Update's comment on the same ordering issue).
        // All spawning - including the initial fill - happens in Tick, which only ever runs
        // after every object's Awake and after ctx.Resolver is assigned.
        public override void Initialize(SpawnRuleContext ctx)
        {
            _active.Clear();

            _segmentHeight = wallPrefab != null
                ? Mathf.Max(0.01f, wallPrefab.GetComponent<Collider2D>().bounds.size.y)
                : 1f;

            _nextSegmentY = ctx.Player != null ? ctx.Player.position.y : 0f;
            _hasFilledInitial = false;
        }

        public override void Tick(SpawnRuleContext ctx, float deltaTime)
        {
            if (ctx.Player == null) return;

            _active.RemoveAll(t => t == null);

            if (!_hasFilledInitial)
            {
                for (int i = 0; i < initialSegmentCount; i++)
                {
                    SpawnSegment(ctx);
                }
                _hasFilledInitial = true;
            }
            else
            {
                float topY = _nextSegmentY - _segmentHeight;
                if (ctx.Player.position.y + refillLookahead >= topY)
                {
                    for (int i = 0; i < refillSegmentCount; i++)
                    {
                        SpawnSegment(ctx);
                    }
                }
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Transform segment = _active[i];
                if (ctx.Player.position.y - segment.position.y > cullDistanceBelowPlayer)
                {
                    Object.Destroy(segment.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }

        private void SpawnSegment(SpawnRuleContext ctx)
        {
            if (wallPrefab == null) return;

            float halfWidth = Mathf.Max(0f, ctx.RawScreenHalfWidth - xInset);
            float y = _nextSegmentY;

            var left = ctx.Resolver.Instantiate(wallPrefab, new Vector3(-halfWidth, y, 0f), Quaternion.identity);
            _active.Add(left.transform);

            var right = ctx.Resolver.Instantiate(wallPrefab, new Vector3(halfWidth, y, 0f), Quaternion.identity);
            _active.Add(right.transform);

            _nextSegmentY += _segmentHeight;
        }
    }
}
