using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nestlabs.Level.Rules
{
    // Fires on a timer with optional ramp-down and a concurrency cap, mirroring
    // ProjectileObstacleSpawner's old timing behavior.
    public abstract class IntervalSpawnRuleSO : SpawnRuleSO
    {
        [Header("Spawn Timing")]
        [SerializeField] private float initialSpawnInterval = 60f;
        [SerializeField] private float minSpawnInterval = 5f;
        [SerializeField] private float intervalDecreasePerSpawn = 2f;
        [SerializeField] private int maxConcurrent = 1;

        private float _currentInterval;
        private float _timer;
        private readonly List<Component> _active = new();

        public override void Initialize(SpawnRuleContext ctx)
        {
            _currentInterval = initialSpawnInterval;
            _timer = 0f;
            _active.Clear();
        }

        public override void Tick(SpawnRuleContext ctx, float deltaTime)
        {
            _active.RemoveAll(c => c == null);

            if (_active.Count >= maxConcurrent) return;

            _timer += deltaTime;
            if (_timer < _currentInterval) return;

            _timer = 0f;
            OnSpawn(ctx, c => _active.Add(c));
            _currentInterval = Mathf.Max(minSpawnInterval, _currentInterval - intervalDecreasePerSpawn);
        }

        protected abstract void OnSpawn(SpawnRuleContext ctx, Action<Component> register);
    }
}
