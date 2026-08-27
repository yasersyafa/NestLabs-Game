using System.Collections.Generic;
using Nestlabs.Level;
using Nestlabs.Obstacle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NestLabs
{
    /// <summary>
    /// Keyboard-driven obstacle test rig. Spawns one obstacle prefab per key press, wiring it the
    /// same way the spawn rules do (Configure + IPoolable.OnSpawned) but without the pool or the
    /// LevelGenerator context. Each press clears the previously spawned obstacle so you always
    /// look at exactly one.
    ///
    ///   W - Swing obstacle
    ///   A - Looping obstacle (MovingObstacle, yoyo travel)
    ///   D - Projectile obstacle (random: enters from the right or the left)
    ///   G - Idle obstacle
    ///   C - Clear
    /// </summary>
    public sealed class ObstacleDebugSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private SwingObstacle _swingPrefab;
        [SerializeField] private MovingObstacle _loopingPrefab;
        [SerializeField] private ProjectileObstacle _projectilePrefab;
        [SerializeField] private IdleObstacle _idlePrefab;

        [Header("Placement (world units, around origin)")]
        [Tooltip("Swing anchor height above origin.")]
        [SerializeField] private float _swingAnchorY = 4f;
        [Tooltip("Half-distance the looping obstacle travels left/right of origin.")]
        [SerializeField] private float _loopHalfSpan = 4f;
        [Tooltip("X the projectile starts from (mirrored to the far side as its end point).")]
        [SerializeField] private float _projectileSpawnX = 7f;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private string _last = "none";
        private GUIStyle _style;

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.wKey.wasPressedThisFrame) SpawnSwing();
            else if (kb.aKey.wasPressedThisFrame) SpawnLooping();
            else if (kb.dKey.wasPressedThisFrame) SpawnProjectile();
            else if (kb.gKey.wasPressedThisFrame) SpawnIdle();
            else if (kb.cKey.wasPressedThisFrame) { Clear(); _last = "cleared"; }
        }

        private void SpawnSwing()
        {
            if (_swingPrefab == null) return;
            Clear();

            var anchor = new Vector3(0f, _swingAnchorY, 0f);
            SwingObstacle instance = Instantiate(_swingPrefab, anchor, Quaternion.identity);
            instance.Configure(anchor);
            (instance as IPoolable)?.OnSpawned(null);

            Track(instance.gameObject);
            _last = "Swing";
        }

        private void SpawnLooping()
        {
            if (_loopingPrefab == null) return;
            Clear();

            var start = new Vector3(-_loopHalfSpan, 0f, 0f);
            var end = new Vector3(_loopHalfSpan, 0f, 0f);
            MovingObstacle instance = Instantiate(_loopingPrefab, start, Quaternion.identity);
            instance.Configure(start, end);
            (instance as IPoolable)?.OnSpawned(null);

            Track(instance.gameObject);
            _last = "Looping";
        }

        private void SpawnProjectile()
        {
            if (_projectilePrefab == null) return;
            Clear();

            bool fromRight = Random.value > 0.5f;
            float startX = fromRight ? _projectileSpawnX : -_projectileSpawnX;
            var start = new Vector3(startX, 0f, 0f);
            var end = new Vector3(-startX, 0f, 0f);

            ProjectileObstacle instance = Instantiate(_projectilePrefab, start, Quaternion.identity);
            instance.Configure(start, end, null);
            GameObject go = instance.gameObject;
            (instance as IPoolable)?.OnSpawned(() =>
            {
                _spawned.Remove(go);
                if (go != null) Destroy(go);
            });

            Track(go);
            _last = fromRight ? "Projectile (from right)" : "Projectile (from left)";
        }

        private void SpawnIdle()
        {
            if (_idlePrefab == null) return;
            Clear();

            IdleObstacle instance = Instantiate(_idlePrefab, Vector3.zero, Quaternion.identity);
            (instance as IPoolable)?.OnSpawned(null);

            Track(instance.gameObject);
            _last = "Idle";
        }

        private void Track(GameObject go) => _spawned.Add(go);

        private void Clear()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null) Destroy(go);
            }
            _spawned.Clear();
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 16, richText = false };
            GUI.Label(new Rect(12f, 8f, 900f, 24f),
                "W Swing   |   A Looping   |   D Projectile (random side)   |   G Idle   |   C Clear", _style);
            GUI.Label(new Rect(12f, 32f, 900f, 24f), $"Last: {_last}", _style);
        }
    }
}
