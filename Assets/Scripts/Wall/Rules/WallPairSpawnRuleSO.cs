using System.Collections.Generic;
using Nestlabs.Level.Rules;
using UnityEngine;
using VContainer.Unity;

namespace Nestlabs.Wall.Rules
{
    // Wall is "always there" climbable terrain, not a one-at-a-time hazard, so it doesn't fit
    // DistanceSpawnRuleSO's single-fire-per-gap model. Instead this tiles wallPrefab's own
    // collider height edge-to-edge into a continuous mirrored left+right column, pre-filling
    // initialSegmentCount pairs immediately, then extending pair by pair whenever the player
    // gets within refillLookahead of the topmost spawned segment.
    [CreateAssetMenu(fileName = "WallPairSpawnRule", menuName = "NestLabs/Wall/Rules/Wall Pair Spawn Rule")]
    public sealed class WallPairSpawnRuleSO : SpawnRuleSO
    {
        [Header("References")]
        [SerializeField] private WallTerrain wallPrefab;

        [Header("Spawn Position")]
        [Tooltip("Extra margin kept inside the screen edge (world units) to account for wall sprite size.")]
        [SerializeField] private float xInset = 0f;

        [Tooltip("Leave at 0 to derive height from wallPrefab's BoxCollider2D. Set only to override.")]
        [SerializeField] private float segmentHeightOverride = 0f;

        [Header("Fill")]
        [Tooltip("How many mirrored pairs to spawn immediately when the level starts.")]
        [SerializeField] private int initialSegmentCount = 5;
        [Tooltip("Refill once the player gets this close to the topmost spawned segment.")]
        [SerializeField] private float refillLookahead = 8f;
        [Tooltip("Hard ceiling on segments spawned in one frame. Guards against bad height data.")]
        [SerializeField] private int maxSegmentsPerFrame = 8;

        [Header("Despawn")]
        [Tooltip("Destroy a segment once the player has climbed this far above it.")]
        [SerializeField] private float cullDistanceBelowPlayer = 12f;

        private float _segmentHeight;
        private float _nextSegmentY;
        private bool _hasFilledInitial;
        private bool _loggedBadHeight;
        private readonly List<Transform> _active = new();

        // Only resets pure state here - deliberately does NOT spawn anything. This runs from
        // LevelGenerator.Awake(), before VContainer's [Inject] Construct() is guaranteed to have
        // set ctx.Resolver (see LevelGenerator.Update's comment on the same ordering issue).
        // All spawning - including the initial fill - happens in Tick, which only ever runs
        // after every object's Awake and after ctx.Resolver is assigned.
        public override void Initialize(SpawnRuleContext ctx)
        {
            _active.Clear();

            _segmentHeight = ResolveSegmentHeight();
            _nextSegmentY = ctx.Player != null ? ctx.Player.position.y : 0f;
            _hasFilledInitial = false;
            _loggedBadHeight = false;
        }

        // Collider2D.bounds is world-space physics state and a prefab asset has no physics shape,
        // so it reads back zero here. size is serialized data, so it is valid straight off the asset.
        // Deliberately returns 0 rather than a fallback - a silent floor here tiles the column in
        // near-zero steps, which never clears refillLookahead and spawns every frame forever.
        private float ResolveSegmentHeight()
        {
            if (segmentHeightOverride > 0f) return segmentHeightOverride;
            if (wallPrefab == null) return 0f;

            if (!wallPrefab.TryGetComponent(out BoxCollider2D box)) return 0f;

            return box.size.y * Mathf.Abs(wallPrefab.transform.lossyScale.y);
        }

        // Fills the starting wall column before the game is playing, so the corridor is visible
        // during the ready pose. Tick still refills as the player climbs.
        public override void Prime(SpawnRuleContext ctx)
        {
            if (ctx.Player == null || _hasFilledInitial) return;
            if (!HasUsableHeight()) return;

            _active.RemoveAll(t => t == null);
            for (int i = 0; i < initialSegmentCount; i++)
            {
                SpawnSegment(ctx);
            }
            _hasFilledInitial = true;
        }

        private bool HasUsableHeight()
        {
            if (_segmentHeight > 0f) return true;

            if (!_loggedBadHeight)
            {
                Debug.LogError($"[WallPairSpawnRule] Could not resolve segment height from {wallPrefab}. " +
                               "Needs a root BoxCollider2D or a segmentHeightOverride. Wall spawning disabled.");
                _loggedBadHeight = true;
            }
            return false;
        }

        public override void Tick(SpawnRuleContext ctx, float deltaTime)
        {
            if (ctx.Player == null) return;

            if (!HasUsableHeight()) return;

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
                // A big instant jump (e.g. a grapple dash) can leave the filled column far behind
                // the player - normal climbing never does this, since topY is always kept ahead
                // by refillLookahead. Snap forward instead of crawling back to the player one
                // small gap per frame, so the next batch starts flush with the player immediately.
                float topY = _nextSegmentY - _segmentHeight;
                if (topY <= ctx.Player.position.y)
                {
                    _nextSegmentY = ctx.Player.position.y;
                    topY = _nextSegmentY - _segmentHeight;
                }

                // Condition-driven, not a fixed batch: keep extending until the column actually
                // clears the lookahead. The budget is a hard stop so bad height data can never
                // spin this every frame again.
                int budget = Mathf.Max(1, maxSegmentsPerFrame);
                while (ctx.Player.position.y + refillLookahead >= topY && budget-- > 0)
                {
                    SpawnSegment(ctx);
                    topY = _nextSegmentY - _segmentHeight;
                }
            }

            // Measured from the segment's top edge, not its center - a segment is taller than the
            // camera is, so culling on center distance recycles walls while they are still on screen.
            float halfSegment = _segmentHeight * 0.5f;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Transform segment = _active[i];
                if (ctx.Player.position.y - (segment.position.y + halfSegment) > cullDistanceBelowPlayer)
                {
                    ctx.Despawn(segment);
                    _active.RemoveAt(i);
                }
            }
        }

        private void SpawnSegment(SpawnRuleContext ctx)
        {
            if (wallPrefab == null) return;

            float halfWidth = Mathf.Max(0f, ctx.RawScreenHalfWidth - xInset);
            float y = _nextSegmentY;

            var left = ctx.Spawn(wallPrefab, new Vector3(-halfWidth, y, 0f), Quaternion.identity);
            _active.Add(left.transform);

            var right = ctx.Spawn(wallPrefab, new Vector3(halfWidth, y, 0f), Quaternion.identity);
            _active.Add(right.transform);

            _nextSegmentY += _segmentHeight;
        }
    }
}
