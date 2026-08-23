using System.Collections.Generic;
using UnityEngine;

namespace Nestlabs.Obstacle
{
    public class ProjectileObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProjectileObstacle projectilePrefab;
        [SerializeField] private RectTransform warningIconPrefab;
        [SerializeField] private RectTransform warningCanvas;
        [SerializeField] private Transform player;

        [Header("Spawn Timing")]
        [SerializeField] private float initialSpawnInterval = 60f;
        [SerializeField] private float minSpawnInterval = 5f;
        [SerializeField] private float intervalDecreasePerSpawn = 2f;
        [SerializeField] private int maxConcurrentProjectiles = 1;

        [Header("Spawn Position")]
        [Tooltip("Projectile travels from this X to the opposite X (e.g. 5 -> starts at +5 or -5, ends at the other side).")]
        [SerializeField] private float spawnXDistance = 5f;
        [Tooltip("Random Y offset added to the player's current Y so projectiles don't always spawn exactly on the player's lane.")]
        [SerializeField] private Vector2 yOffsetRange = new Vector2(-2f, 2f);

        private float currentInterval;
        private float timer;
        private readonly List<ProjectileObstacle> activeProjectiles = new();

        private void Awake()
        {
            currentInterval = initialSpawnInterval;
        }

        private void Update()
        {
            activeProjectiles.RemoveAll(p => p == null);

            if (activeProjectiles.Count >= maxConcurrentProjectiles) return;

            timer += Time.deltaTime;
            if (timer < currentInterval) return;

            timer = 0f;
            SpawnProjectile();
            currentInterval = Mathf.Max(minSpawnInterval, currentInterval - intervalDecreasePerSpawn);
        }

        private void SpawnProjectile()
        {
            if (projectilePrefab == null || player == null) return;

            bool spawnFromRight = Random.value > 0.5f;
            float startX = spawnFromRight ? spawnXDistance : -spawnXDistance;
            float endX = -startX;
            float spawnY = player.position.y + Random.Range(yOffsetRange.x, yOffsetRange.y);

            var startPos = new Vector3(startX, spawnY, 0f);
            var endPos = new Vector3(endX, spawnY, 0f);

            RectTransform warningIcon = null;
            if (warningIconPrefab != null && warningCanvas != null)
            {
                warningIcon = Instantiate(warningIconPrefab, warningCanvas);
                warningIcon.gameObject.SetActive(false);
            }

            var instance = Instantiate(projectilePrefab, startPos, Quaternion.identity);
            instance.Configure(startPos, endPos, warningIcon);
            activeProjectiles.Add(instance);
        }
    }
}
