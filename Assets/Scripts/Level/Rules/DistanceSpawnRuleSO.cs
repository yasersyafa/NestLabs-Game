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

        [Header("Despawn")]
        [Tooltip("Destroy the obstacle once the player has climbed this far above it.")]
        [SerializeField] private float cullDistanceBelowPlayer = 12f;

        private float _nextSpawnY;
        private readonly List<Transform> _active = new();

        public override void Initialize(SpawnRuleContext ctx)
        {
            _nextSpawnY = ctx.Player != null ? ctx.Player.position.y + lookaheadDistance : lookaheadDistance;
            _active.Clear();
        }

        public override void Tick(SpawnRuleContext ctx, float deltaTime)
        {
            if (ctx.Player == null) return;

            _active.RemoveAll(t => t == null);

            if (ctx.Player.position.y + lookaheadDistance >= _nextSpawnY)
            {
                float spawnY = _nextSpawnY + UnityEngine.Random.Range(yOffsetRange.x, yOffsetRange.y);
                OnSpawn(spawnY, ctx, t => _active.Add(t));
                _nextSpawnY += UnityEngine.Random.Range(spawnYGapMin, spawnYGapMax);
            }

            CullActive(ctx);
        }

        private void CullActive(SpawnRuleContext ctx)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Transform obstacle = _active[i];
                if (ctx.Player.position.y - obstacle.position.y > cullDistanceBelowPlayer)
                {
                    UnityEngine.Object.Destroy(obstacle.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }

        // Called once per fire. Implementations call register once for a single obstacle,
        // or more than once (e.g. a mirrored left/right pair) if one fire spawns several instances.
        protected abstract void OnSpawn(float spawnY, SpawnRuleContext ctx, Action<Transform> register);
    }
}
