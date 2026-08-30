#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Nestlabs.Level;
using Nestlabs.Obstacle;
using Nestlabs.Wall;
using NestLabs.Node;
using NestLabs.Shared.Flow;
using Unity.PerformanceTesting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace NestLabs.Tests.PlayMode
{
    // Perf baseline for the steady-state spawn loop. Same synthetic-climber rig as
    // LevelGeneratorSpawnTests (real wired scene, real DI, real rule assets and pooling), but
    // instead of asserting a rule fired it records per-frame cost over a long ascent so a later
    // optimisation pass has a number to beat. Run with:
    //   unity test --mode PlayMode --filter LevelGeneratorPerfTests
    // then open the Performance Test Report window, or diff test-results.xml against baseline/.
    public class LevelGeneratorPerfTests
    {
        private const string ScenePath = "Assets/Scenes/YaserScene.unity";

        // ~10x the smoke test's climb - long enough for pooling churn (spawn + cull) to reach
        // steady state and for a frame-time median to settle.
        private const int WarmupFrames = 120;
        private const int MeasureFrames = 2000;
        private const float ClimbPerFrame = 0.5f;

        [UnityTest, Performance]
        public IEnumerator SteadyStateSpawnLoop_FrameCost()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var scope = Object.FindFirstObjectByType<LifetimeScope>();
            Assert.IsNotNull(scope, "No LifetimeScope in the scene - DI is not wired");
            scope.Container.Resolve<IGameStateService>().EnterPlay();
            yield return null;

            var generator = Object.FindFirstObjectByType<LevelGenerator>();
            Assert.IsNotNull(generator, "LevelGenerator not found in scene");

            FieldInfo playerField = typeof(LevelGenerator)
                .GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);
            var originalPlayer = (Transform)playerField.GetValue(generator);
            Assert.IsNotNull(originalPlayer, "LevelGenerator.player is unassigned in the scene");

            var climber = new GameObject("TestClimber").transform;
            climber.position = originalPlayer.position;
            playerField.SetValue(generator, climber);

            for (int i = 0; i < WarmupFrames; i++)
            {
                climber.position += Vector3.up * ClimbPerFrame;
                yield return null;
            }

            var markers = new[]
            {
                "LevelGenerator.TickRules",
                "LevelGenerator.SyncTransforms",
            };

            using (Measure.ProfilerMarkers(markers))
            using (Measure.Frames().Scope())
            {
                for (int i = 0; i < MeasureFrames; i++)
                {
                    climber.position += Vector3.up * ClimbPerFrame;
                    yield return null;
                }
            }

            int idle = Object.FindObjectsByType<IdleObstacle>(FindObjectsSortMode.None).Length;
            int moving = Object.FindObjectsByType<MovingObstacle>(FindObjectsSortMode.None).Length;
            int swing = Object.FindObjectsByType<SwingObstacle>(FindObjectsSortMode.None).Length;
            int walls = Object.FindObjectsByType<WallTerrain>(FindObjectsSortMode.None).Length;
            int nodes = Object.FindObjectsByType<NodeBase>(FindObjectsSortMode.None).Length;

            Measure.Custom(new SampleGroup("Active.Obstacles", SampleUnit.Undefined),
                idle + moving + swing);
            Measure.Custom(new SampleGroup("Active.Walls", SampleUnit.Undefined), walls);
            Measure.Custom(new SampleGroup("Active.Nodes", SampleUnit.Undefined), nodes);

            Debug.Log($"[LevelGeneratorPerfTests] climbed to y={climber.position.y:F0} - " +
                      $"obstacles={idle + moving + swing} walls={walls} nodes={nodes}");

            // Loose leak guard - a bounded column at this climb height is tens of segments, not
            // hundreds. Mirrors LevelGeneratorSpawnTests.MaxReasonableWalls, scaled for the run.
            Assert.Less(walls, 200, "Wall column is not bounding itself over the long climb");
        }
    }
}
#endif
