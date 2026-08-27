using System.Collections.Generic;
using System.Linq;
using Nestlabs.Obstacle;
using NestLabs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NestLabs.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/obstacle-debug.unity from scratch: a camera plus an
    /// <see cref="ObstacleDebugSpawner"/> with every obstacle prefab wired in. Run from the menu
    /// or headless:
    ///   unity run . -- -executeMethod NestLabs.EditorTools.DebugObstacleSceneBuilder.Build -quit
    /// </summary>
    public static class DebugObstacleSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/obstacle-debug.unity";
        private const string PrefabDir = "Assets/Prefabs/Obstacle/";

        [MenuItem("NestLabs/Debug/Build Obstacle Debug Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.09f, 0.1f, 0.13f, 1f);
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }

            var go = new GameObject("ObstacleDebug");
            ObstacleDebugSpawner spawner = go.AddComponent<ObstacleDebugSpawner>();

            var so = new SerializedObject(spawner);
            Assign(so, "_swingPrefab", Load<SwingObstacle>("SwingObstacle.prefab"));
            Assign(so, "_loopingPrefab", Load<MovingObstacle>("LoopingObstacle.prefab"));
            Assign(so, "_projectilePrefab", Load<ProjectileObstacle>("ProjectileObstacle.prefab"));
            Assign(so, "_idlePrefab", Load<IdleObstacle>("IdleObstacle.prefab"));
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[DebugObstacleSceneBuilder] Saved {ScenePath}: {saved}");

            AddToBuildSettings(ScenePath);
        }

        private static void Assign(SerializedObject so, string field, Object value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[DebugObstacleSceneBuilder] Missing field '{field}' on ObstacleDebugSpawner.");
                return;
            }
            if (value == null)
            {
                Debug.LogError($"[DebugObstacleSceneBuilder] Prefab for '{field}' not found under {PrefabDir}.");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static T Load<T>(string prefabFile) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(PrefabDir + prefabFile);

        private static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
