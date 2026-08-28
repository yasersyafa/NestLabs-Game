using System.Collections.Generic;
using Nestlabs.Level;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace Nestlabs.Level.Rules
{
    // Shared per-frame data handed to every rule's Tick. Built once by LevelGenerator.Awake;
    // RawScreenHalfWidth is recomputed once per frame so all rules see the same camera state
    // without each duplicating the cam.orthographicSize * cam.aspect calculation themselves.
    //
    // Also the single pooling entry point for every rule: Spawn/Despawn replace raw
    // Instantiate/Destroy so obstacle/wall/node churn doesn't repeatedly allocate and GC. One
    // ObjectPool<Component> per distinct prefab reference, created lazily on first use.
    public sealed class SpawnRuleContext
    {
        public Transform Player;
        public IObjectResolver Resolver;
        public Camera Cam;
        public RectTransform UiCanvas;
        public float RawScreenHalfWidth;

        // World Y below which content can never be reached again, so recycling it is safe. Tracks
        // the rising hazard line, and falls back to a player-relative distance in scenes with no
        // hazard. Culling against this instead of the player's live Y is what lets a falling player
        // land back on walls and nodes they already passed.
        public float CullFloorY = float.NegativeInfinity;

        // Set whenever a Spawn/Despawn moves or toggles a collider. LevelGenerator flushes it with
        // a single Physics2D.SyncTransforms after all rules tick - the sync cost scales with total
        // collider count, so one per frame beats one per spawned instance.
        public bool TransformsDirty;

        private readonly Dictionary<Component, IObjectPool<Component>> _poolsByPrefab = new();

        // Keyed by GameObject (not the pooled component itself) so Despawn can be called with
        // *any* Component on that GameObject - a Transform (what the distance-based rules track
        // in their _active lists) or the behavior script itself (what Projectile tracks) both
        // resolve to the same pooled instance and its pool.
        private readonly Dictionary<GameObject, (Component Instance, IObjectPool<Component> Pool)> _byGameObject = new();

        // Spots a later rule must not spawn into. Grapple nodes claim their grab radius so hazards
        // never land inside a point the player is required to reach.
        private readonly List<(Transform Owner, float Radius)> _claims = new();

        public void AddClaim(Transform owner, float radius)
        {
            if (owner == null || radius <= 0f) return;
            _claims.Add((owner, radius));
        }

        /// <summary>
        /// Distance from <paramref name="point"/> to the nearest claim's edge. Negative means inside
        /// a claim. <see cref="float.MaxValue"/> when nothing is claimed. Callers compare against
        /// their own required padding, and can rank candidate positions when none fully clear.
        /// </summary>
        public float ClaimClearance(Vector2 point)
        {
            float nearest = float.MaxValue;

            // Pruned lazily here rather than in Despawn: a pooled instance goes inactive, not null,
            // and Despawn has no idea whether its instance ever claimed anything.
            for (int i = _claims.Count - 1; i >= 0; i--)
            {
                (Transform owner, float radius) = _claims[i];
                if (owner == null || !owner.gameObject.activeInHierarchy)
                {
                    _claims.RemoveAt(i);
                    continue;
                }

                float edge = Vector2.Distance(owner.position, point) - radius;
                if (edge < nearest) nearest = edge;
            }

            return nearest;
        }

        // Does not call IPoolable.OnSpawned - callers must Configure(...) first (when the type
        // has one) before triggering that, so setup never races stale/default field values.
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (!_poolsByPrefab.TryGetValue(prefab, out IObjectPool<Component> pool))
            {
                pool = new ObjectPool<Component>(
                    // Position/rotation here are a throwaway initial placement - every Get()
                    // result (fresh or reused) is immediately repositioned below.
                    createFunc: () => Resolver.Instantiate(prefab, Vector3.zero, Quaternion.identity),
                    actionOnGet: instance => instance.gameObject.SetActive(true),
                    actionOnRelease: instance => instance.gameObject.SetActive(false),
                    actionOnDestroy: instance => Object.Destroy(instance.gameObject));
                _poolsByPrefab[prefab] = pool;
            }

            var component = (T)pool.Get();
            component.transform.SetPositionAndRotation(position, rotation);

            // A reused instance's Collider2D keeps its *previous* physics bounds until Physics2D
            // syncs transforms on its own schedule (next FixedUpdate) - a same-frame query like
            // PlayerSensor's Rigidbody2D.Cast would otherwise hit it at its old, stale location.
            TransformsDirty = true;

            _byGameObject[component.gameObject] = (component, pool);
            return component;
        }

        public void Despawn(Component instance)
        {
            if (instance == null) return;
            if (!_byGameObject.TryGetValue(instance.gameObject, out var entry)) return;

            // Drop the mapping before releasing: keeps the dictionary from growing for the whole
            // run, and turns a double Despawn into a no-op instead of an ObjectPool collectionCheck
            // throw.
            _byGameObject.Remove(instance.gameObject);

            (entry.Instance as IPoolable)?.OnDespawned();
            entry.Pool.Release(entry.Instance);
            TransformsDirty = true;
        }
    }
}
