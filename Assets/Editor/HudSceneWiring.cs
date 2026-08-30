using NestLabs.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NestLabs.EditorTools
{
    /// <summary>
    /// One-shot wiring for the HUD overlay: puts <see cref="HudPanelController"/> on GUI.prefab with
    /// every panel and button reference filled in, then replaces the stale hand-copied GUI objects in
    /// dev-awe.unity with a real prefab instance. Run from the menu or headless:
    ///   unity run . -- -executeMethod NestLabs.EditorTools.HudSceneWiring.Wire -quit
    /// Re-running is safe: the controller is reused if present and the scene copy is rebuilt.
    /// </summary>
    public static class HudSceneWiring
    {
        private const string PrefabPath = "Assets/Prefabs/UI/GUI.prefab";

        // test-merge is the scene actually being played; dev-awe is the Build Settings entry. Both
        // carry a GameLifetimeScope, so both need the HUD or their container skips the registration.
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/test-merge.unity",
            "Assets/Scenes/dev-awe.unity",
        };

        [MenuItem("NestLabs/Debug/Wire HUD Panels")]
        public static void Wire()
        {
            if (!WirePrefab()) return;

            foreach (string scenePath in ScenePaths)
            {
                WireScene(scenePath);
            }

            Debug.Log("[HudSceneWiring] Done.");
        }

        [MenuItem("NestLabs/Debug/Wire HUD Panels (prefab only)")]
        public static void WirePrefabOnly()
        {
            if (WirePrefab()) Debug.Log("[HudSceneWiring] Prefab wiring done.");
        }

        private static bool WirePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[HudSceneWiring] Could not load '{PrefabPath}'.");
                return false;
            }

            try
            {
                var controller = root.GetComponent<HudPanelController>();
                if (controller == null) controller = root.AddComponent<HudPanelController>();

                var so = new SerializedObject(controller);
                bool ok = true;

                ok &= SetObject(so, root, "overlay", "Overlay");
                ok &= SetObject(so, root, "pausePanel", "Overlay/Pause");
                ok &= SetObject(so, root, "creditsPanel", "Overlay/Credits");
                ok &= SetObject(so, root, "diedPanel", "Overlay/Died");

                ok &= SetButton(so, root, "pauseButton", "TopArea/Pause Button");
                ok &= SetButton(so, root, "resumeButton", "Overlay/Pause/MainPanel/Content/ButtonResume");
                ok &= SetButton(so, root, "pauseExitButton", "Overlay/Pause/MainPanel/ExitButton");
                ok &= SetButton(so, root, "creditButton", "Overlay/Pause/MainPanel/Content/HLayout/CreditButton");
                ok &= SetButton(so, root, "creditsExitButton", "Overlay/Credits/MainPanel/ExitButton");
                ok &= SetButton(so, root, "retryButton", "Overlay/Died/MainPanel/Content/ButtonRetry");
                ok &= SetButton(so, root, "homeButton", "Overlay/Died/MainPanel/Content/ButtonHome");

                if (!ok)
                {
                    Debug.LogError("[HudSceneWiring] Aborted: some references could not be resolved.");
                    return false;
                }

                if (!WireDiedScorePanel(root))
                {
                    Debug.LogError("[HudSceneWiring] Aborted: DiedScorePanel references could not be resolved.");
                    return false;
                }

                const string InvertMatPath = "Assets/Art/Materials/UIInvert.mat";
                var invertMat = AssetDatabase.LoadAssetAtPath<Material>(InvertMatPath);
                so.FindProperty("buttonInvertMaterial").objectReferenceValue = invertMat;
                if (invertMat == null)
                    Debug.LogWarning($"[HudSceneWiring] '{InvertMatPath}' not found; hover invert left unwired.");

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[HudSceneWiring] Wired HudPanelController on '{PrefabPath}'.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // DiedScorePanel sits on the same GUI root as HudPanelController: the game-over panel
        // starts inactive, so a script on the panel itself would never get injected and would miss
        // the finalize event. It only needs the two labels inside the Died panel.
        private static bool WireDiedScorePanel(GameObject root)
        {
            var panel = root.GetComponent<DiedScorePanel>();
            if (panel == null) panel = root.AddComponent<DiedScorePanel>();

            var so = new SerializedObject(panel);
            bool ok = true;

            ok &= SetComponent<TMPro.TMP_Text>(so, root, "finalScoreText",
                "Overlay/Died/MainPanel/Content/ScoreText");
            ok &= SetObject(so, root, "newBestLabel",
                "Overlay/Died/MainPanel/Content/HighscoreLabel");

            if (!ok) return false;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[HudSceneWiring] Wired DiedScorePanel.");
            return true;
        }

        private static void WireScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Transform hud = FindByName(scene, "HUD");
            if (hud == null)
            {
                Debug.LogError($"[HudSceneWiring] No 'HUD' object in '{scenePath}'.");
                return;
            }

            // The scene carried a hand-copied GUI (missing the whole Overlay subtree) plus an empty
            // PauseMenu placeholder. Both are superseded by the prefab instance.
            for (int i = hud.childCount - 1; i >= 0; i--)
            {
                Transform child = hud.GetChild(i);
                if (child.name == "GUI" || child.name == "PauseMenu")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hud);
            instance.name = "GUI";
            instance.transform.SetAsFirstSibling();

            var rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[HudSceneWiring] Instanced '{PrefabPath}' under HUD in '{scenePath}'.");
        }

        private static Transform FindByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root.transform;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name) return t;
                }
            }
            return null;
        }

        private static bool SetObject(SerializedObject so, GameObject root, string field, string path)
        {
            Transform t = root.transform.Find(path);
            if (t == null)
            {
                Debug.LogError($"[HudSceneWiring] Missing path '{path}' for field '{field}'.");
                return false;
            }

            so.FindProperty(field).objectReferenceValue = t.gameObject;
            return true;
        }

        private static bool SetComponent<T>(SerializedObject so, GameObject root, string field, string path)
            where T : Component
        {
            Transform t = root.transform.Find(path);
            if (t == null)
            {
                Debug.LogError($"[HudSceneWiring] Missing path '{path}' for field '{field}'.");
                return false;
            }

            var component = t.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"[HudSceneWiring] No {typeof(T).Name} on '{path}' for field '{field}'.");
                return false;
            }

            so.FindProperty(field).objectReferenceValue = component;
            return true;
        }

        private static bool SetButton(SerializedObject so, GameObject root, string field, string path)
        {
            Transform t = root.transform.Find(path);
            if (t == null)
            {
                Debug.LogError($"[HudSceneWiring] Missing path '{path}' for field '{field}'.");
                return false;
            }

            var button = t.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"[HudSceneWiring] No Button on '{path}' for field '{field}'.");
                return false;
            }

            so.FindProperty(field).objectReferenceValue = button;
            return true;
        }
    }
}
