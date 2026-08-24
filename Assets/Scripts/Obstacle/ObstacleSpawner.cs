using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Nestlabs.Obstacle
{
    // Spawns Idle and Moving obstacles off a single shared height progression so the
    // two types never land on overlapping Y bands. ProjectileObstacle is spawned by its
    // own ProjectileObstacleSpawner since it's time-based, not tied to player progress.
    public class ObstacleSpawner : MonoBehaviour
    {
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        [Header("References")]
        [SerializeField] private MovingObstacle movingObstaclePrefab;
        [SerializeField] private IdleObstacle idleObstaclePrefab;
        [SerializeField] private Transform player;

        [Header("Spawn Height")]
        [Tooltip("Spawn the next obstacle once the player gets this far below it.")]
        [SerializeField] private float lookaheadDistance = 8f;
        [SerializeField] private float spawnYGapMin = 4f;
        [SerializeField] private float spawnYGapMax = 6f;
        [Tooltip("Random jitter added on top of the computed spawn height.")]
        [SerializeField] private Vector2 yOffsetRange = new Vector2(-1f, 1f);

        [Header("Spawn Position")]
        [Tooltip("Extra margin kept inside the screen edge (world units) to account for obstacle sprite size.")]
        [SerializeField] private float xEdgeMargin = 0.5f;

        private Camera cam;

        [Header("Type Selection")]
        [Range(0f, 1f)]
        [SerializeField] private float idleSpawnChance = 0.5f;

        [Header("Despawn")]
        [Tooltip("Destroy the obstacle once the player has climbed this far above it.")]
        [SerializeField] private float cullDistanceBelowPlayer = 12f;

        private float nextSpawnY;
        private readonly List<Transform> active = new();

        private void Awake()
        {
            cam = Camera.main;
            if (player != null) nextSpawnY = player.position.y + lookaheadDistance;
        }

        // Half-width of the visible screen in world units, minus edge margin. Recomputed
        // per spawn (not cached) so it stays correct if resolution/aspect changes at runtime.
        // This is the sole source of the X range - obstacles are placed/moved purely
        // within actual screen bounds, no separate configurable X range.
        private float GetScreenHalfWidth()
        {
            if (cam == null || !cam.orthographic) return 0f;

            float screenHalfWidth = cam.orthographicSize * cam.aspect;
            return Mathf.Max(0f, screenHalfWidth - xEdgeMargin);
        }

        private void Update()
        {
            if (player == null) return;

            active.RemoveAll(t => t == null);

            if (player.position.y + lookaheadDistance >= nextSpawnY)
            {
                float spawnY = nextSpawnY + Random.Range(yOffsetRange.x, yOffsetRange.y);

                if (Random.value < idleSpawnChance)
                    SpawnIdle(spawnY);
                else
                    SpawnMoving(spawnY);

                nextSpawnY += Random.Range(spawnYGapMin, spawnYGapMax);
            }

            CullObstacles();
        }

        private void SpawnMoving(float spawnY)
        {
            if (movingObstaclePrefab == null) return;

            float halfWidth = GetScreenHalfWidth();
            float startX = Random.Range(-halfWidth, halfWidth);
            float endX = Random.Range(-halfWidth, halfWidth);

            var startPos = new Vector3(startX, spawnY, 0f);
            var endPos = new Vector3(endX, spawnY, 0f);

            var instance = _resolver.Instantiate(movingObstaclePrefab, startPos, Quaternion.identity);
            instance.Configure(startPos, endPos);
            active.Add(instance.transform);
        }

        private void SpawnIdle(float spawnY)
        {
            if (idleObstaclePrefab == null) return;

            float halfWidth = GetScreenHalfWidth();
            float x = Random.Range(-halfWidth, halfWidth);
            var instance = _resolver.Instantiate(idleObstaclePrefab, new Vector3(x, spawnY, 0f), Quaternion.identity);
            active.Add(instance.transform);
        }

        private void CullObstacles()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var obstacle = active[i];
                if (player.position.y - obstacle.position.y > cullDistanceBelowPlayer)
                {
                    Destroy(obstacle.gameObject);
                    active.RemoveAt(i);
                }
            }
        }
    }
}
