using UnityEngine;
using VContainer;

namespace Nestlabs.Level.Rules
{
    // Shared per-frame data handed to every rule's Tick. Built once by LevelGenerator.Awake;
    // RawScreenHalfWidth is recomputed once per frame so all rules see the same camera state
    // without each duplicating the cam.orthographicSize * cam.aspect calculation themselves.
    public sealed class SpawnRuleContext
    {
        public Transform Player;
        public IObjectResolver Resolver;
        public Camera Cam;
        public RectTransform UiCanvas;
        public float RawScreenHalfWidth;
    }
}
