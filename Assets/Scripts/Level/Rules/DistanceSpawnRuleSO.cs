using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nestlabs.Level.Rules
{
    // Fires based on how far the player has climbed, using the same nextSpawnY progression
    // ObstacleSpawner used to own. Subclasses only implement OnSpawn - everything about the
    // progression and culling is handled here so every distance-based rule behaves consistently.
    public abstract class DistanceSpawnRuleSO : SpawnRuleSO
    {
        [Header("Spawn Height")]
        [Tooltip("Spawn the next obstacle once the player gets this far below it.")]
        [SerializeField] private float lookaheadDistance = 8f;
        [SerializeField] private float spawnYGapMin = 4f;
        [SerializeField] private float spawnYGapMax = 6f;
        [Tooltip("Random jitter added on top of the computed spawn height.")]
        [SerializeField] private Vector2 yOffsetRange = new Vector2(-1f, 1f);
        [Tooltip("Hard floor on the distance between two consecutive spawns, applied after yOffsetRange jitter. 0 = off.")]
        [SerializeField] private float minSpawnSeparation = 0f;

        [Header("Offscreen Guarantee")]
        [Tooltip("Never spawn inside the camera view. Raises the effective lookahead to at least this far above the top of the viewport.")]
        [SerializeField] private bool keepSpawnsOffscreen = false;
        [SerializeField] private float offscreenMargin = 2f;

        [Header("Despawn")]
        [Tooltip("Destroy the obstacle once the player has climbed this far above it.")]
        [SerializeField] private float cullDistanceBelowPlayer = 12f;

        [Header("Initial Fill")]
        [Tooltip("How many times to fire immediately when the level starts (e.g. so a few are already visible), before settling into the normal gap-based cadence. 1 = old behavior (just the first one fires right away).")]
        [SerializeField] private int initialBurstCount = 1;
        [Tooltip("Y above the player where the initial burst starts filling, walking upward by the normal gap. 0 = start at lookaheadDistance (previous behavior).")]
        [SerializeField] private float initialFillStartOffset = 0f;

        private float _nextSpawnY;
        private bool _hasBurstFilled;
        private float _lastSpawnY;
        private bool _hasLastSpawnY;
        private readonly List<Transform> _active = new();

        // Only resets pure state here - deliberately does NOT spawn anything. This runs from
        // LevelGenerator.Awake(), before VContainer's [Inject] Construct() is guaranteed to have
        // set ctx.Resolver. All spawning - including the initial burst - happens in Tick, which
        // only ever runs after every object's Awake and after ctx.Resolver is assigned.
        public override void Initialize(SpawnRuleContext ctx)
        {
            // The burst starts here rather than at lookaheadDistance when initialFillStartOffset is
            // set, so a rule with a long lookahead doesn't begin the run with an empty corridor
            // between the player and its first spawn.
            float startOffset = initialFillStartOffset > 0f ? initialFillStartOffset : lookaheadDistance;

            _nextSpawnY = ctx.Player != null ? ctx.Player.position.y + startOffset : startOffset;
            _active.Clear();
            _hasBurstFilled = false;
            _hasLastSpawnY = false;
        }

        // Follows the live camera instead of a hardcoded number, so this stays correct across
        // aspect ratios, orthographic size changes, and camera follow offset changes.
        private float EffectiveLookahead(SpawnRuleContext ctx)
        {
            if (!keepSpawnsOffscreen || ctx.Cam == null || !ctx.Cam.orthographic || ctx.Player == null)
                return lookaheadDistance;

            float viewTopAbovePlayer =
                (ctx.Cam.transform.position.y + ctx.Cam.orthographicSize) - ctx.Player.position.y;

            return Mathf.Max(lookaheadDistance, viewTopAbovePlayer + offscreenMargin);
        }

        public override void Tick(SpawnRuleContext ctx, float deltaTime)
        {
            if (ctx.Player == null) return;

            _active.RemoveAll(t => t == null);

            if (!_hasBurstFilled)
            {
                for (int i = 0; i < Mathf.Max(1, initialBurstCount); i++)
                {
                    FireOnce(ctx);
                }
                _hasBurstFilled = true;
            }
            else
            {
                // A big instant jump (e.g. a grapple dash) can leave _nextSpawnY far behind the
                // player - normal climbing never does this, since _nextSpawnY is always kept
                // ahead by lookaheadDistance. Snap forward instead of crawling back to the player
                // one small gap per frame, so spawning resumes right at the player immediately.
                float lookahead = EffectiveLookahead(ctx);

                if (_nextSpawnY <= ctx.Player.position.y)
                {
                    _nextSpawnY = ctx.Player.position.y + lookahead;
                }

                if (ctx.Player.position.y + lookahead >= _nextSpawnY)
                {
                    FireOnce(ctx);
                }
            }

            CullActive(ctx);
        }

        private void FireOnce(SpawnRuleContext ctx)
        {
            float spawnY = _nextSpawnY + UnityEngine.Random.Range(yOffsetRange.x, yOffsetRange.y);

            // Steady state only - the initial burst is allowed to fill the visible corridor, since
            // at level start there is nothing on screen for the player to have watched appear.
            if (_hasBurstFilled && keepSpawnsOffscreen && ctx.Cam != null && ctx.Cam.orthographic)
            {
                float floorY = ctx.Cam.transform.position.y + ctx.Cam.orthographicSize + offscreenMargin;
                if (spawnY < floorY) spawnY = floorY;
            }

            if (_hasLastSpawnY && spawnY < _lastSpawnY + minSpawnSeparation)
            {
                spawnY = _lastSpawnY + minSpawnSeparation;
            }

            OnSpawn(spawnY, ctx, t => _active.Add(t));

            _lastSpawnY = spawnY;
            _hasLastSpawnY = true;

            // A clamped spawn can land above the ladder cursor. Without this the next gap is
            // measured from a position already passed, re-creating the tight pair just prevented.
            if (_nextSpawnY < spawnY) _nextSpawnY = spawnY;
            _nextSpawnY += UnityEngine.Random.Range(spawnYGapMin, spawnYGapMax);
        }

        private void CullActive(SpawnRuleContext ctx)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Transform obstacle = _active[i];
                if (ctx.Player.position.y - obstacle.position.y > cullDistanceBelowPlayer)
                {
                    ctx.Despawn(obstacle);
                    _active.RemoveAt(i);
                }
            }
        }

        // Called once per fire. Implementations call register once for a single obstacle,
        // or more than once (e.g. a mirrored left/right pair) if one fire spawns several instances.
        protected abstract void OnSpawn(float spawnY, SpawnRuleContext ctx, Action<Transform> register);
    }
}
