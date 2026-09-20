using System;
using System.Collections.Generic;
using NestLabs.Shared.Culling;
using UnityEngine;

namespace Nestlabs.Level.Rules
{
    /// <summary>
    /// Base rule for objects that spawn based on time intervals.
    /// Supports decreasing intervals over time and active instance concurrency caps.
    /// </summary>
    public abstract class IntervalSpawnRuleSO : SpawnRuleSO
    {
        [Header("Spawn Timing")]
        [Min(0.1f)]
        [SerializeField] private float initialSpawnInterval = 60f;

        [Min(0.1f)]
        [SerializeField] private float minSpawnInterval = 5f;

        [Min(0f)]
        [SerializeField] private float intervalDecreasePerSpawn = 2f;

        [Min(1)]
        [SerializeField] private int maxConcurrent = 1;

        private float _currentInterval;
        private float _timer;

        private readonly List<Component> _active = new();

        public override void Initialize(SpawnRuleContext ctx)
        {
            _currentInterval =
                Mathf.Max(0.1f, initialSpawnInterval);

            _timer = 0f;
            _active.Clear();
        }

        public override void Tick(
            SpawnRuleContext ctx,
            float deltaTime)
        {
            RemoveDestroyedInstances();
            UpdateInstanceVisibility(ctx);

            if (_active.Count >= maxConcurrent)
            {
                return;
            }

            // Subclasses can hold the timer until a specific condition is met,
            // such as reaching a minimum altitude for projectiles.
            if (!CanSpawn(ctx))
            {
                _timer = 0f;
                return;
            }

            _timer += deltaTime;

            if (_timer < _currentInterval)
            {
                return;
            }

            _timer = 0f;

            OnSpawn(
                ctx,
                component =>
                {
                    if (component != null)
                    {
                        _active.Add(component);
                    }
                });

            _currentInterval = Mathf.Max(
                minSpawnInterval,
                _currentInterval - intervalDecreasePerSpawn);
        }

        /// <summary>
        /// Additional condition before the spawn timer starts running.
        /// Always allowed by default.
        /// </summary>
        protected virtual bool CanSpawn(SpawnRuleContext ctx)
        {
            return true;
        }

        protected abstract void OnSpawn(
            SpawnRuleContext ctx,
            Action<Component> register);

        /// <summary>
        /// Called by objects that finish on their own,
        /// such as projectiles after leaving the screen.
        /// </summary>
        protected void ReleaseFromActive(Component instance)
        {
            if (instance != null)
            {
                _active.Remove(instance);
            }
        }

        private void RemoveDestroyedInstances()
        {
            _active.RemoveAll(component => component == null);
        }

        private void UpdateInstanceVisibility(
            SpawnRuleContext ctx)
        {
            foreach (Component instance in _active)
            {
                if (instance is not IScreenVisibility visibility)
                {
                    continue;
                }

                bool shouldBeVisible = ctx.IsYVisible(
                    instance.transform.position.y,
                    visibility.IsScreenVisible);

                if (shouldBeVisible !=
                    visibility.IsScreenVisible)
                {
                    visibility.SetScreenVisible(
                        shouldBeVisible);
                }
            }
        }

        private void OnValidate()
        {
            initialSpawnInterval =
                Mathf.Max(0.1f, initialSpawnInterval);

            minSpawnInterval = Mathf.Clamp(
                minSpawnInterval,
                0.1f,
                initialSpawnInterval);

            intervalDecreasePerSpawn =
                Mathf.Max(0f, intervalDecreasePerSpawn);

            maxConcurrent =
                Mathf.Max(1, maxConcurrent);
        }
    }
}
