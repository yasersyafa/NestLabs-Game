using System.IO;
using Nestlabs.Chunk;
using Nestlabs.Chunk.Rules;
using Nestlabs.Level;
using Nestlabs.Level.Rules;
using Nestlabs.Obstacle;
using NestLabs.Node;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NestLabs.EditorTools
{
    /// <summary>
    /// One-time content build for the chunk-based node/obstacle spawn system (see CLAUDE.md ->
    /// Level generation). Creates the initial 6 <see cref="ChunkSO"/> assets plus the
    /// "CoreChunks" <see cref="ChunkSpawnRuleSO"/> instance, then repoints LevelGenerator.prefab's
    /// rules list - and every wired scene's live LevelGenerator instance - from
    /// CoreObstacles_WeightedGroup/GrapplePoints onto it. Run from the menu or headless:
    ///   unity run . -- -executeMethod NestLabs.EditorTools.ChunkAuthoringTool.Build -quit
    /// Idempotent: re-running overwrites the same assets/wiring rather than duplicating them.
    /// Chunk X/Y numbers are authored by hand against referenceHalfWidth (the narrowest supported
    /// corridor, ~3.4 at 9:16) - see the design notes on each Build*Chunk call below for the
    /// clearance math each layout was checked against.
    /// </summary>
    public static class ChunkAuthoringTool
    {
        private const string ChunkDir = "Assets/ScriptableObjects/Chunk/";
        private const string RuleDir = "Assets/ScriptableObjects/Chunk/Rules/";
        private const string ObstacleDir = "Assets/Prefabs/Obstacle/";
        private const string NodeDir = "Assets/Prefabs/Node/";

        private const string LevelGeneratorPrefabPath = "Assets/Prefabs/Level/LevelGenerator.prefab";
        private const string SideWallsRulePath = "Assets/ScriptableObjects/Wall/Rules/SideWalls_Pair.asset";
        private const string ProjectileRulePath = "Assets/ScriptableObjects/Obstacle/Rules/Projectile_Ramping.asset";

        // dev-awe.unity carries no per-scene rules override today (inherits the prefab default),
        // so it needs no explicit rewrite - included anyway so a rewrite is idempotent if one
        // ever gets added by hand later. GameScene.unity is undocumented in CLAUDE.md but live and
        // enabled in Build Settings; included per explicit decision to bring it into the cutover.
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/YaserScene.unity",
            "Assets/Scenes/test-merge.unity",
            "Assets/Scenes/dev-awe.unity",
            "Assets/Scenes/GameScene.unity",
        };

        private struct EntryDef
        {
            public ChunkSO.EntryType type;
            public float x, y;
            public float jitterMin, jitterMax;
            public float movingEndX;
            public NodeBase nodePrefab;
            public IdleObstacle idlePrefab;
            public MovingObstacle movingPrefab;
            public SwingObstacle swingPrefab;
        }

        [MenuItem("NestLabs/Debug/Build Chunk Content")]
        public static void Build()
        {
            EnsureFolder(ChunkDir);
            EnsureFolder(RuleDir);

            NodeBase nodeDefault = Load<NodeBase>(NodeDir + "Node_Base.prefab");
            NodeBase nodeStrong = Load<NodeBase>(NodeDir + "Variants/Node_Strong.prefab");
            IdleObstacle idle = Load<IdleObstacle>(ObstacleDir + "IdleObstacle.prefab");
            MovingObstacle moving = Load<MovingObstacle>(ObstacleDir + "LoopingObstacle.prefab");
            SwingObstacle swing = Load<SwingObstacle>(ObstacleDir + "SwingObstacle.prefab");

            if (nodeDefault == null || nodeStrong == null || idle == null || moving == null || swing == null)
            {
                Debug.LogError("[ChunkAuthoringTool] Missing a source prefab under Assets/Prefabs/Node or " +
                                "Assets/Prefabs/Obstacle - aborting.");
                return;
            }

            // Default node claim = radius 2, Strong = radius 3 (NodeData_Default/Strong.asset).
            // Obstacle collider radius ~0.3, chunk clearance padding target 0.5 -> required
            // separation ~2.8 (Default) / ~3.8 (Strong). Every entry below clears that with a
            // buffer against its own jitter range, computed by hand per chunk.

            // Calm/breather pair - generous spacing, doubles as the low end of the pool per the
            // "fold calm patterns into the same pool" design (no separate breather system).
            ChunkSO easyA = BuildChunk("Chunk_EasyA", ChunkTier.Easy, 1f, 5f, new[]
            {
                Node(nodeDefault, 0f, 2f),
                Idle(idle, -2.6f, 4.5f, 0.2f),   // worst-case dist to node ~3.46 (>2.8 required)
            });

            ChunkSO easyB = BuildChunk("Chunk_EasyB", ChunkTier.Easy, 1f, 4f, new[]
            {
                Node(nodeDefault, -1.5f, 1.5f),  // no obstacle at all
            });

            ChunkSO midA = BuildChunk("Chunk_MidA", ChunkTier.Mid, 1f, 5.4f, new[]
            {
                Node(nodeDefault, 1.2f, 2f),
                Idle(idle, -2.8f, 1f, 0.2f),               // worst-case dist ~3.93
                Moving(moving, -1f, 5f, 2.6f, 0.2f),       // start ~3.61, end ~3.31
            });

            ChunkSO midB = BuildChunk("Chunk_MidB", ChunkTier.Mid, 1f, 5.4f, new[]
            {
                Node(nodeDefault, -1.2f, 2f),
                Idle(idle, 2.6f, 1.2f, 0.2f),              // worst-case dist ~3.79
                Moving(moving, 2.4f, 5f, -2.4f, 0.2f),     // start ~4.53, end ~3.23
            });

            // Strong node (radius 3) - the exact case that used to blow the corridor width.
            // Node held fixed (no jitter) so the hand-checked distances stay exact; obstacles get
            // small jitter accounted for in the worst-case numbers below. A wide vertical offset
            // does the work a narrow corridor can't do on X alone.
            ChunkSO hardA = BuildChunk("Chunk_HardA", ChunkTier.Hard, 1f, 7f, new[]
            {
                Node(nodeStrong, 0f, 3f),
                Idle(idle, -3.2f, 6.5f, 0.2f),   // worst-case dist ~4.61 (>3.8 required)
                Idle(idle, 3.2f, 0f, 0.2f),      // worst-case dist ~4.24
                Swing(swing, 3.2f, 6f, 0.15f),   // worst-case dist ~4.28
            });

            ChunkSO hardB = BuildChunk("Chunk_HardB", ChunkTier.Hard, 1f, 7.2f, new[]
            {
                Node(nodeStrong, 1f, 2.5f),
                // Moving obstacle's sweep line is a full 4.3 above the node's Y, so even its
                // closest approach (dx=0, directly overhead) clears 3.8 - unlike the old
                // WeightedGroupSpawnRuleSO, which only ever checked the sweep's start point.
                Moving(moving, -3f, 6.8f, 2.8f, 0.2f),
                Swing(swing, -2.8f, 0f, 0.15f),  // worst-case dist ~4.42
                Idle(idle, 3.4f, 0f, 0.15f),     // worst-case dist ~4.10
            });

            var pool = new[] { easyA, easyB, midA, midB, hardA, hardB };
            ChunkSpawnRuleSO rule = BuildRule(pool);

            WirePrefab(rule);
            foreach (string scenePath in ScenePaths) WireScene(scenePath, rule);

            AssetDatabase.SaveAssets();
            Debug.Log("[ChunkAuthoringTool] Done.");
        }

        // ---- entry factories ----

        private static EntryDef Node(NodeBase prefab, float x, float y) => new EntryDef
        {
            type = ChunkSO.EntryType.Node, x = x, y = y, nodePrefab = prefab,
        };

        private static EntryDef Idle(IdleObstacle prefab, float x, float y, float jitter) => new EntryDef
        {
            type = ChunkSO.EntryType.IdleObstacle, x = x, y = y, idlePrefab = prefab,
            jitterMin = -jitter, jitterMax = jitter,
        };

        private static EntryDef Moving(MovingObstacle prefab, float x, float y, float endX, float jitter) => new EntryDef
        {
            type = ChunkSO.EntryType.MovingObstacle, x = x, y = y, movingPrefab = prefab, movingEndX = endX,
            jitterMin = -jitter, jitterMax = jitter,
        };

        private static EntryDef Swing(SwingObstacle prefab, float x, float y, float jitter) => new EntryDef
        {
            type = ChunkSO.EntryType.SwingObstacle, x = x, y = y, swingPrefab = prefab,
            jitterMin = -jitter, jitterMax = jitter,
        };

        // ---- asset construction ----

        private static ChunkSO BuildChunk(string assetName, ChunkTier tier, float weight, float footprintHeight, EntryDef[] entries)
        {
            string path = ChunkDir + assetName + ".asset";
            var chunk = AssetDatabase.LoadAssetAtPath<ChunkSO>(path);
            if (chunk == null)
            {
                chunk = ScriptableObject.CreateInstance<ChunkSO>();
                AssetDatabase.CreateAsset(chunk, path);
            }

            var so = new SerializedObject(chunk);
            so.FindProperty("chunkId").stringValue = assetName;
            so.FindProperty("tier").enumValueIndex = (int)tier;
            so.FindProperty("weight").floatValue = weight;
            so.FindProperty("footprintHeight").floatValue = footprintHeight;
            so.FindProperty("referenceHalfWidth").floatValue = 3.4f;

            SerializedProperty entriesProp = so.FindProperty("entries");
            entriesProp.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty e = entriesProp.GetArrayElementAtIndex(i);
                EntryDef def = entries[i];
                e.FindPropertyRelative("type").enumValueIndex = (int)def.type;
                e.FindPropertyRelative("relativeX").floatValue = def.x;
                e.FindPropertyRelative("relativeY").floatValue = def.y;
                e.FindPropertyRelative("jitterX").vector2Value = new Vector2(def.jitterMin, def.jitterMax);
                e.FindPropertyRelative("nodePrefab").objectReferenceValue = def.nodePrefab;
                e.FindPropertyRelative("idlePrefab").objectReferenceValue = def.idlePrefab;
                e.FindPropertyRelative("movingPrefab").objectReferenceValue = def.movingPrefab;
                e.FindPropertyRelative("movingEndRelativeX").floatValue = def.movingEndX;
                e.FindPropertyRelative("swingPrefab").objectReferenceValue = def.swingPrefab;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(chunk);
            return chunk;
        }

        private static ChunkSpawnRuleSO BuildRule(ChunkSO[] pool)
        {
            string path = RuleDir + "CoreChunks.asset";
            var rule = AssetDatabase.LoadAssetAtPath<ChunkSpawnRuleSO>(path);
            if (rule == null)
            {
                rule = ScriptableObject.CreateInstance<ChunkSpawnRuleSO>();
                AssetDatabase.CreateAsset(rule, path);
            }

            var so = new SerializedObject(rule);

            // DistanceSpawnRuleSO base fields. Gap wider than either legacy rule's own (obstacle
            // 6-9, node 4-6) since one fire now places a whole layout, not a single hazard.
            // minSpawnSeparation is the load-bearing number here: it's a hard floor (see
            // DistanceSpawnRuleSO.FireOnce), so it must clear the tallest chunk's footprintHeight
            // (7.2, Chunk_HardB) plus the largest required node/obstacle clearance (~3.8, a Strong
            // node) with margin - otherwise two consecutive chunks could place a node from one and
            // an obstacle from the next close enough in Y to violate clearance despite each chunk
            // being solvable on its own. 13 leaves ~5.8 of guaranteed cross-chunk Y gap.
            so.FindProperty("lookaheadDistance").floatValue = 20f;
            so.FindProperty("spawnYGapMin").floatValue = 14f;
            so.FindProperty("spawnYGapMax").floatValue = 18f;
            so.FindProperty("yOffsetRange").vector2Value = new Vector2(-1f, 1f);
            so.FindProperty("minSpawnSeparation").floatValue = 13f;
            so.FindProperty("keepSpawnsOffscreen").boolValue = false;
            so.FindProperty("offscreenMargin").floatValue = 2f;
            so.FindProperty("initialBurstCount").intValue = 3;
            so.FindProperty("initialFillStartOffset").floatValue = 4f;

            SerializedProperty poolProp = so.FindProperty("pool");
            poolProp.arraySize = pool.Length;
            for (int i = 0; i < pool.Length; i++)
            {
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
            }

            // Easy has no ramp entry below -> RampMultiplier's default (always full weight).
            SerializedProperty ramps = so.FindProperty("tierRamps");
            ramps.arraySize = 2;
            SetRamp(ramps.GetArrayElementAtIndex(0), ChunkTier.Mid, 20f, 45f);
            SetRamp(ramps.GetArrayElementAtIndex(1), ChunkTier.Hard, 60f, 110f);

            so.FindProperty("antiRepeatWindow").intValue = 3;
            so.FindProperty("xEdgeMargin").floatValue = 0.5f;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rule);
            return rule;
        }

        private static void SetRamp(SerializedProperty p, ChunkTier tier, float introduce, float full)
        {
            p.FindPropertyRelative("tier").enumValueIndex = (int)tier;
            p.FindPropertyRelative("introduceAtDistance").floatValue = introduce;
            p.FindPropertyRelative("fullWeightAtDistance").floatValue = full;
        }

        // ---- wiring ----

        private static void WirePrefab(ChunkSpawnRuleSO chunkRule)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(LevelGeneratorPrefabPath);
            try
            {
                var gen = root.GetComponent<LevelGenerator>();
                if (gen == null)
                {
                    Debug.LogError($"[ChunkAuthoringTool] No LevelGenerator on {LevelGeneratorPrefabPath}.");
                    return;
                }

                if (!SetRulesList(new SerializedObject(gen), chunkRule)) return;
                PrefabUtility.SaveAsPrefabAsset(root, LevelGeneratorPrefabPath);
                Debug.Log($"[ChunkAuthoringTool] Rewired {LevelGeneratorPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireScene(string scenePath, ChunkSpawnRuleSO chunkRule)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[ChunkAuthoringTool] {scenePath} does not exist - skipping.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var gen = Object.FindFirstObjectByType<LevelGenerator>();
            if (gen == null)
            {
                Debug.LogWarning($"[ChunkAuthoringTool] No LevelGenerator found in {scenePath} - skipping.");
                return;
            }

            if (!SetRulesList(new SerializedObject(gen), chunkRule)) return;

            // When the 3 new values happen to match LevelGenerator.prefab's own new default
            // (true whenever a scene had no prior per-index override), SerializedObject correctly
            // writes no override at all rather than duplicate the default - but that leaves any
            // *pre-existing, unrelated* override (e.g. a leftover 'rules.Array.data[3]' from the
            // old 4-entry array) untouched, since nothing about writing an unrelated property
            // prunes it. Explicitly drop any out-of-range rules.Array.data[N] modification so no
            // stale index can resurface later.
            PurgeStaleRulesOverrides(gen.gameObject, 3);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ChunkAuthoringTool] Rewired {scenePath}.");
        }

        // Setting the property through the normal SerializedObject API (rather than hand-editing
        // PrefabInstance.m_Modifications YAML) is what keeps this safe - Unity reconciles the
        // instance override correctly, including clearing/replacing any stale trailing array
        // entries from the old 4-rule list.
        private static bool SetRulesList(SerializedObject so, ChunkSpawnRuleSO chunkRule)
        {
            SpawnRuleSO wallRule = AssetDatabase.LoadAssetAtPath<SpawnRuleSO>(SideWallsRulePath);
            SpawnRuleSO projectileRule = AssetDatabase.LoadAssetAtPath<SpawnRuleSO>(ProjectileRulePath);

            if (wallRule == null || projectileRule == null)
            {
                Debug.LogError("[ChunkAuthoringTool] Could not load SideWalls_Pair.asset or " +
                                "Projectile_Ramping.asset.");
                return false;
            }

            SerializedProperty rules = so.FindProperty("rules");
            if (rules == null)
            {
                Debug.LogError("[ChunkAuthoringTool] Target has no 'rules' field - is this a LevelGenerator?");
                return false;
            }

            rules.arraySize = 3;
            rules.GetArrayElementAtIndex(0).objectReferenceValue = wallRule;
            rules.GetArrayElementAtIndex(1).objectReferenceValue = projectileRule;
            rules.GetArrayElementAtIndex(2).objectReferenceValue = chunkRule;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Drops any 'rules.Array.data[N]' (N >= validSize) modification recorded against the
        // outermost PrefabInstance this GameObject belongs to. Out-of-range modifications are
        // inert at merge time (Unity has nothing to apply them to), but leaving them in the YAML
        // is exactly the kind of dangling entry that turned confusing during this migration - see
        // ChunkAuthoringTool's header comment and the "GameScene.unity" case it was found in.
        private static void PurgeStaleRulesOverrides(GameObject member, int validSize)
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(member);
            if (root == null) return;

            PropertyModification[] mods = PrefabUtility.GetPropertyModifications(root);
            if (mods == null) return;

            var kept = new System.Collections.Generic.List<PropertyModification>(mods.Length);
            bool changed = false;
            foreach (PropertyModification m in mods)
            {
                if (IsOutOfRangeRulesEntry(m.propertyPath, validSize))
                {
                    changed = true;
                    continue;
                }
                kept.Add(m);
            }

            if (changed) PrefabUtility.SetPropertyModifications(root, kept.ToArray());
        }

        private static bool IsOutOfRangeRulesEntry(string propertyPath, int validSize)
        {
            const string prefix = "rules.Array.data[";
            if (!propertyPath.StartsWith(prefix)) return false;

            int end = propertyPath.IndexOf(']', prefix.Length);
            if (end < 0) return false;

            return int.TryParse(propertyPath.Substring(prefix.Length, end - prefix.Length), out int idx)
                   && idx >= validSize;
        }

        // ---- misc ----

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
