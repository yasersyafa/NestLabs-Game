using System;
using Nestlabs.Level;
using Nestlabs.Level.Rules;
using UnityEngine;
using VContainer.Unity;

namespace Nestlabs.Obstacle.Rules
{
    [CreateAssetMenu(
        fileName = "ProjectileSpawnRule",
        menuName =
            "NestLabs/Obstacle/Rules/Projectile Spawn Rule")]
    public sealed class ProjectileSpawnRuleSO
        : IntervalSpawnRuleSO
    {
        [Header("References")]
        [SerializeField]
        private ProjectileObstacle projectilePrefab;

        [SerializeField]
        private RectTransform warningIconPrefab;

        [Header("Difficulty")]
        [Tooltip(
            "Projectiles only become active after the player has climbed " +
            "this far from their starting position.")]
        [Min(0f)]
        [SerializeField]
        private float minimumClimbedDistance = 30f;

        [Header("Spawn Position")]
        [Tooltip(
            "Horizontal distance of the projectile's starting point " +
            "from the center of the arena.")]
        [Min(0.1f)]
        [SerializeField]
        private float spawnXDistance = 5f;

        [Tooltip(
            "Projectiles cannot spawn closer to the player's Y position " +
            "than this value.")]
        [Min(0f)]
        [SerializeField]
        private float minimumVerticalOffset = 1.5f;

        [Tooltip(
            "Maximum vertical distance of the projectile " +
            "from the player's Y position.")]
        [Min(0f)]
        [SerializeField]
        private float maximumVerticalOffset = 3f;

        private float _startingPlayerY;
        private float _highestPlayerY;

        public override void Initialize(SpawnRuleContext ctx)
        {
            base.Initialize(ctx);

            if (ctx.Player != null)
            {
                _startingPlayerY = ctx.Player.position.y;
                _highestPlayerY = _startingPlayerY;
            }
            else
            {
                _startingPlayerY = 0f;
                _highestPlayerY = 0f;
            }
        }

        protected override bool CanSpawn(
            SpawnRuleContext ctx)
        {
            if (ctx.Player == null ||
                projectilePrefab == null)
            {
                return false;
            }

            _highestPlayerY = Mathf.Max(
                _highestPlayerY,
                ctx.Player.position.y);

            float climbedDistance =
                Mathf.Max(
                    0f,
                    _highestPlayerY - _startingPlayerY);

            return climbedDistance >=
                   minimumClimbedDistance;
        }

        protected override void OnSpawn(
            SpawnRuleContext ctx,
            Action<Component> register)
        {
            if (projectilePrefab == null ||
                ctx.Player == null)
            {
                return;
            }

            bool spawnFromRight =
                UnityEngine.Random.value > 0.5f;

            float startX = spawnFromRight
                ? spawnXDistance
                : -spawnXDistance;

            float endX = -startX;

            float verticalOffset =
                GetSafeVerticalOffset();

            float spawnY =
                ctx.Player.position.y + verticalOffset;

            Vector3 startPosition =
                new Vector3(startX, spawnY, 0f);

            Vector3 endPosition =
                new Vector3(endX, spawnY, 0f);

            RectTransform warningIcon =
                CreateWarningIcon(ctx);

            ProjectileObstacle instance = ctx.Spawn(
                projectilePrefab,
                startPosition,
                Quaternion.identity);

            instance.Configure(
                startPosition,
                endPosition,
                warningIcon);

            register(instance);

            if (instance is IPoolable poolable)
            {
                poolable.OnSpawned(
                    () =>
                    {
                        ReleaseFromActive(instance);
                        ctx.Despawn(instance);
                    });
            }
        }

        private float GetSafeVerticalOffset()
        {
            float minimum =
                Mathf.Max(0f, minimumVerticalOffset);

            float maximum =
                Mathf.Max(minimum, maximumVerticalOffset);

            float magnitude =
                UnityEngine.Random.Range(
                    minimum,
                    maximum);

            float direction =
                UnityEngine.Random.value > 0.5f
                    ? 1f
                    : -1f;

            return magnitude * direction;
        }

        private RectTransform CreateWarningIcon(
            SpawnRuleContext ctx)
        {
            if (warningIconPrefab == null ||
                ctx.UiCanvas == null)
            {
                return null;
            }

            RectTransform warningIcon =
                UnityEngine.Object.Instantiate(
                    warningIconPrefab,
                    ctx.UiCanvas);

            warningIcon.gameObject.SetActive(false);

            return warningIcon;
        }

        private void OnValidate()
        {
            minimumClimbedDistance =
                Mathf.Max(0f, minimumClimbedDistance);

            spawnXDistance =
                Mathf.Max(0.1f, spawnXDistance);

            minimumVerticalOffset =
                Mathf.Max(0f, minimumVerticalOffset);

            maximumVerticalOffset =
                Mathf.Max(
                    minimumVerticalOffset,
                    maximumVerticalOffset);
        }
    }
}
