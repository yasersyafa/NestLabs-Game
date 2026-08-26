using System;
using Nestlabs.Level.Rules;
using UnityEngine;
using VContainer.Unity;

namespace Nestlabs.Obstacle.Rules
{
    // Ports ProjectileObstacleSpawner's behavior verbatim onto the interval timing base.
    // warningCanvas intentionally is NOT a field here - a .asset can't durably hold a
    // reference to a scene RectTransform (it nulls out on reload). It comes from
    // ctx.UiCanvas, set by LevelGenerator from its own scene-level field.
    [CreateAssetMenu(fileName = "ProjectileSpawnRule", menuName = "NestLabs/Obstacle/Rules/Projectile Spawn Rule")]
    public sealed class ProjectileSpawnRuleSO : IntervalSpawnRuleSO
    {
        [Header("References")]
        [SerializeField] private ProjectileObstacle projectilePrefab;
        [SerializeField] private RectTransform warningIconPrefab;

        [Header("Spawn Position")]
        [Tooltip("Projectile travels from this X to the opposite X (e.g. 5 -> starts at +5 or -5, ends at the other side).")]
        [SerializeField] private float spawnXDistance = 5f;
        [Tooltip("Random Y offset added to the player's current Y so projectiles don't always spawn exactly on the player's lane.")]
        [SerializeField] private Vector2 yOffsetRange = new Vector2(-2f, 2f);

        protected override void OnSpawn(SpawnRuleContext ctx, Action<Component> register)
        {
            if (projectilePrefab == null || ctx.Player == null) return;

            bool spawnFromRight = UnityEngine.Random.value > 0.5f;
            float startX = spawnFromRight ? spawnXDistance : -spawnXDistance;
            float endX = -startX;
            float spawnY = ctx.Player.position.y + UnityEngine.Random.Range(yOffsetRange.x, yOffsetRange.y);

            var startPos = new Vector3(startX, spawnY, 0f);
            var endPos = new Vector3(endX, spawnY, 0f);

            RectTransform warningIcon = null;
            if (warningIconPrefab != null && ctx.UiCanvas != null)
            {
                warningIcon = UnityEngine.Object.Instantiate(warningIconPrefab, ctx.UiCanvas);
                warningIcon.gameObject.SetActive(false);
            }

            var instance = ctx.Resolver.Instantiate(projectilePrefab, startPos, Quaternion.identity);
            instance.Configure(startPos, endPos, warningIcon);
            register(instance);
        }
    }
}
