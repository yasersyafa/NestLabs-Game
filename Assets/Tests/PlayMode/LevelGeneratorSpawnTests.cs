#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Nestlabs.Level;
using Nestlabs.Obstacle;
using Nestlabs.Wall;
using NestLabs.Node;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NestLabs.Tests.PlayMode
{
    // Smoke test for the data-driven level generator: loads the real wired scene (so DI,
    // rule assets, and prefab references all go through exactly as they do in the game),
    // then drives a synthetic climber Transform - bypassing the player's own physics/state
    // machine, which we don't want to fight here - to confirm every rule still fires.
    public class LevelGeneratorSpawnTests
    {
        private const string ScenePath = "Assets/Scenes/YaserScene.unity";
        private const int ClimbFrames = 300;
        private const float ClimbPerFrame = 0.5f;
        private const int MaxReasonableWalls = 60;

        // Mirrors CoreObstacles_WeightedGroup.asset - the test needs the same corridor the rule uses.
        private const float ObstacleXEdgeMargin = 1f;
        private const float NodeClearancePadding = 0.5f;

        [UnityTest]
        public IEnumerator RulesSpawnObstaclesAndWallPairsAsClimberAscends()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var generator = Object.FindFirstObjectByType<LevelGenerator>();
            Assert.IsNotNull(generator, "LevelGenerator not found in scene - is the LevelGenerator prefab wired in?");

            FieldInfo playerField = typeof(LevelGenerator).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);
            var originalPlayer = (Transform)playerField.GetValue(generator);
            Assert.IsNotNull(originalPlayer, "LevelGenerator.player is unassigned in the scene");

            var climber = new GameObject("TestClimber").transform;
            climber.position = originalPlayer.position;
            playerField.SetValue(generator, climber);

            for (int i = 0; i < ClimbFrames; i++)
            {
                climber.position += Vector3.up * ClimbPerFrame;
                yield return null;
            }

            int idleCount = Object.FindObjectsByType<IdleObstacle>(FindObjectsSortMode.None).Length;
            int movingCount = Object.FindObjectsByType<MovingObstacle>(FindObjectsSortMode.None).Length;
            int swingCount = Object.FindObjectsByType<SwingObstacle>(FindObjectsSortMode.None).Length;
            WallTerrain[] walls = Object.FindObjectsByType<WallTerrain>(FindObjectsSortMode.None);
            int wallCount = walls.Length;
            int nodeCount = Object.FindObjectsByType<NodeBase>(FindObjectsSortMode.None).Length;

            Debug.Log($"[LevelGeneratorSpawnTests] climbed to y={climber.position.y:F1} - " +
                      $"idle={idleCount} moving={movingCount} swing={swingCount} wall={wallCount} node={nodeCount}");

            Assert.Greater(idleCount + movingCount + swingCount, 0,
                "WeightedGroupSpawnRuleSO never fired an Idle/Moving/Swing obstacle over the climb");
            Assert.Greater(wallCount, 0,
                "WallPairSpawnRuleSO never fired a wall pair over the climb");
            Assert.AreEqual(0, wallCount % 2,
                "Wall pair rule should always spawn left+right together (expected an even count)");
            // Regression guard: a bad segment height makes the refill gate never close, which
            // spawns a pair every frame. The climb covers 150 world units, so a correctly tiled
            // column is on the order of tens of segments, never hundreds.
            Assert.Less(wallCount, MaxReasonableWalls,
                $"WallPairSpawnRuleSO spawned {wallCount} active walls over a {ClimbFrames}-frame climb - " +
                "the column is not bounding itself, check the resolved segment height");
            Assert.Greater(nodeCount, 0,
                "NodeSpawnRuleSO never fired a grapple node over the climb");

            AssertObstaclesClearOfNodeRadii();

            AssertWallsOnSolidLayer(walls);
        }

        // Idle only. Moving tweens along X and Swing arcs through its rope, so their live positions
        // drift away from the spawn point - and only the spawn footprint is avoidance-checked, so
        // asserting on those two would flag the sweep case that is deliberately out of scope.
        private static void AssertObstaclesClearOfNodeRadii()
        {
            Camera cam = Camera.main;
            NodeBase[] nodes = Object.FindObjectsByType<NodeBase>(FindObjectsSortMode.None);
            IdleObstacle[] idle = Object.FindObjectsByType<IdleObstacle>(FindObjectsSortMode.None);

            if (cam == null || !cam.orthographic || nodes.Length == 0 || idle.Length == 0) return;

            float corridor = 2f * Mathf.Max(0f, cam.orthographicSize * cam.aspect - ObstacleXEdgeMargin);

            foreach (IdleObstacle obstacle in idle)
            {
                float obstacleRadius = obstacle.TryGetComponent(out CircleCollider2D circle)
                    ? circle.radius * Mathf.Abs(obstacle.transform.lossyScale.x)
                    : 0f;

                foreach (NodeBase node in nodes)
                {
                    float minSeparation = node.ClaimRadius + obstacleRadius;

                    // Avoidance is best-effort by design: at 9:16 a single node's clearance spans
                    // more X than the whole playable corridor, so no clear spot exists to pick.
                    // Only assert where one did.
                    if (2f * (minSeparation + NodeClearancePadding) >= corridor) continue;

                    float actual = Vector2.Distance(obstacle.transform.position, node.Position);

                    Assert.Greater(actual, minSeparation,
                        $"IdleObstacle at {obstacle.transform.position} is {actual:F2} from node " +
                        $"at {node.Position} but needs more than {minSeparation:F2} - it is inside the " +
                        $"grapple radius, and the {corridor:F2}-wide corridor had room to avoid it");
                }
            }
        }

        private static void AssertWallsOnSolidLayer(WallTerrain[] walls)
        {
            const int solidLayer = 6;
            foreach (WallTerrain wall in walls)
            {
                Assert.AreEqual(solidLayer, wall.gameObject.layer,
                    "Spawned WallTerrain must be on the \"Solid\" layer (6) or the player's wall-latch sensor will never detect it");
            }
        }
    }
}
#endif
