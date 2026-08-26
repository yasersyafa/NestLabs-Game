using System.Collections.Generic;
using Nestlabs.Level.Rules;
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

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private RectTransform warningCanvas;

        [Header("Rules")]
        [SerializeField] private List<SpawnRuleSO> rules = new();

        private Camera _cam;
        private SpawnRuleContext _ctx;
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

            float dt = Time.deltaTime;
            foreach (ISpawnRule rule in _runtimeRules)
            {
                rule.Tick(_ctx, dt);
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
