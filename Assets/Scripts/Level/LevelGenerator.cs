using System.Collections.Generic;
using Nestlabs.Level.Rules;
using NestLabs.Shared.Flow;
using NestLabs.Shared.Hazards;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Nestlabs.Level
{
    // Replaces ObstacleSpawner + ProjectileObstacleSpawner. Owns the shared player/camera
    // reference, the single Update loop, and the lifetime of rule runtime clones. Which
    // prefabs spawn and how is entirely defined by the SpawnRuleSO assets assigned in `rules` -
    // adding a new prefab variant of an existing kind or retuning cadence is a data change here,
    // not a code change.
    public sealed class LevelGenerator : MonoBehaviour
    {
        private IObjectResolver _resolver;
        private IGameStateService _gameState = NullGameStateService.Instance;
        private IHazardLine _hazard = NullHazardLine.Instance;

        [Inject]
        public void Construct(
            IObjectResolver resolver, IGameStateService gameState, IHazardLine hazard)
        {
            _resolver = resolver;
            _gameState = gameState ?? NullGameStateService.Instance;
            _hazard = hazard ?? NullHazardLine.Instance;
        }

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private RectTransform warningCanvas;

        [Header("Rules")]
        [SerializeField] private List<SpawnRuleSO> rules = new();

        [Header("Culling")]
        [Tooltip("Cull distance below the player used only in scenes with no hazard line (no fog).")]
        [SerializeField] private float fallbackCullDistanceBelowPlayer = 12f;

        private Camera _cam;
        private SpawnRuleContext _ctx;
        private bool _primed;
        private readonly List<ISpawnRule> _runtimeRules = new();

        private void Awake()
        {
            _cam = Camera.main;
            // Resolver is deliberately left unset here - VContainer's [Inject] Construct() is
            // called during the scene's Awake pass, but not necessarily before this Awake runs
            // (Awake order between GameLifetimeScope and LevelGenerator isn't guaranteed). It's
            // assigned in Update instead, which only ever runs after every object's Awake.
            _ctx = new SpawnRuleContext
            {
                Player = player,
                Cam = _cam,
                UiCanvas = warningCanvas,
            };

            // ScriptableObject assets are shared/persistent - ticking the source asset directly
            // would leak progression/timer state across play sessions (and corrupt it if the
            // same asset were ever assigned to two generators). Each rule gets its own private
            // runtime clone instead; the source .asset stays pure, reusable config.
            foreach (SpawnRuleSO asset in rules)
            {
                if (asset == null) continue;

                var clone = Instantiate(asset);
                clone.Initialize(_ctx);
                _runtimeRules.Add(clone);
            }
        }

        private void Update()
        {
            if (player == null) return;

            _ctx.Player = player;
            _ctx.Resolver = _resolver;
            _ctx.RawScreenHalfWidth = (_cam != null && _cam.orthographic)
                ? _cam.orthographicSize * _cam.aspect
                : 0f;

            // Content is recycled only once the fog has swallowed it, never because the player
            // climbed past it - otherwise falling drops the player into a shaft with no walls or
            // nodes left to recover on. Scenes with no fog keep the old player-relative distance.
            _ctx.CullFloorY = _hazard.IsActive
                ? _hazard.LethalY
                : player.position.y - fallbackCullDistanceBelowPlayer;

            // Fill the opening layout once, before the run starts, so the player can read the
            // first obstacles/nodes/walls during the ready pose instead of only after the first
            // input. Resolver may not be assigned on the very first frame - retry until it is.
            if (!_primed && _resolver != null)
            {
                foreach (ISpawnRule rule in _runtimeRules)
                {
                    rule.Prime(_ctx);
                }
                _primed = true;
                SyncTransformsIfDirty();
            }

            // Death and Pause both leave this Update running, so without this every rule keeps
            // spawning after the run is over.
            if (!_gameState.IsPlaying) return;

            float dt = Time.deltaTime;
            foreach (ISpawnRule rule in _runtimeRules)
            {
                rule.Tick(_ctx, dt);
            }

            SyncTransformsIfDirty();
        }

        // All spawning happens before this, so anything querying physics later in Update order
        // still sees synced colliders.
        private void SyncTransformsIfDirty()
        {
            if (_ctx.TransformsDirty)
            {
                Physics2D.SyncTransforms();
                _ctx.TransformsDirty = false;
            }
        }

        private void OnDestroy()
        {
            foreach (ISpawnRule rule in _runtimeRules)
            {
                if (rule is Object obj) Destroy(obj);
            }
        }
    }
}
