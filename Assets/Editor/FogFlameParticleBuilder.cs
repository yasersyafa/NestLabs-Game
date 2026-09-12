using UnityEditor;
using UnityEngine;

namespace NestLabs.EditorTools
{
    /// <summary>
    /// Builds/reconfigures the "FlameParticles" child on FogSystem.prefab: a Shuriken system
    /// replacing the earlier FogFlameEdge shader attempt. That shader carved noise into
    /// Surface's sprite UV space, but Surface renders at 648x1976 world units (60x400 scale on
    /// a 10.8x4.94 sprite) while the camera only ever sees a 12-unit-tall slice — every UV-based
    /// tuning pass was shaping a region that was almost entirely off-screen. This builds the
    /// particle spawn volume directly in world units around the real lethal line instead
    /// (read from the root BoxCollider2D, not hardcoded), and reverts Surface back to the
    /// default sprite material since it's no longer carrying the effect.
    ///
    /// Hand-authoring a ParticleSystem's YAML directly (the approach used for the shader/material
    /// this session) is high-risk: there is no other ParticleSystem anywhere in this project to
    /// pattern-match against, and Shuriken's serialized shape is large and version-fragile. Every
    /// module here is configured through the public C# API instead, so this can run headless with
    /// no attached editor:
    ///   unity run . -- -executeMethod NestLabs.EditorTools.FogFlameParticleBuilder.Build -quit
    /// Re-running is safe: the existing FlameParticles child (if any) is reused, not duplicated.
    /// </summary>
    public static class FogFlameParticleBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Hazards/FogSystem.prefab";
        private const string ChildName = "FlameParticles";

        // Spawn volume: a box a little below the lethal line up through roughly one viewport
        // height above it, so flame is visible as the line approaches, not only at the exact
        // kill pixel. Bottom/height are authored margins; X width and Y center are derived from
        // the root collider so retuning the collider later doesn't silently desync this.
        private const float BottomMarginBelowLethalY = 1f;
        private const float BoxHeight = 8f;

        [MenuItem("NestLabs/Debug/Build Fog Flame Particles")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[FogFlameParticleBuilder] Could not load '{PrefabPath}'.");
                return;
            }

            try
            {
                var collider = root.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    Debug.LogError("[FogFlameParticleBuilder] FogSystem root has no BoxCollider2D.");
                    return;
                }

                float lethalYLocal = collider.offset.y + collider.size.y * 0.5f;
                float shaftWidth = collider.size.x;
                float boxCenterY = lethalYLocal - BottomMarginBelowLethalY + BoxHeight * 0.5f;

                Transform flame = root.transform.Find(ChildName);
                GameObject flameGo = flame != null ? flame.gameObject : new GameObject(ChildName);
                if (flame == null) flameGo.transform.SetParent(root.transform, false);
                flameGo.transform.localPosition = new Vector3(collider.offset.x, boxCenterY, 0f);
                flameGo.transform.localRotation = Quaternion.identity;
                flameGo.transform.localScale = Vector3.one;

                var ps = flameGo.GetComponent<ParticleSystem>();
                if (ps == null) ps = flameGo.AddComponent<ParticleSystem>();

                ConfigureMain(ps);
                ConfigureEmission(ps);
                ConfigureShape(ps, shaftWidth);
                ConfigureVelocityOverLifetime(ps);
                ConfigureNoise(ps);
                ConfigureSizeOverLifetime(ps);
                ConfigureColorOverLifetime(ps);
                ConfigureRenderer(root, flameGo);

                RevertSurfaceMaterial(root);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[FogFlameParticleBuilder] Wired '{ChildName}' on '{PrefabPath}' " +
                          $"(lethalYLocal={lethalYLocal:F2}, shaftWidth={shaftWidth:F2}).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureMain(ParticleSystem ps)
        {
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 2f);
            main.startColor = new Color(0f, 0f, 0f, 0.85f);
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }

        private static void ConfigureEmission(ParticleSystem ps)
        {
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 50f;
        }

        private static void ConfigureShape(ParticleSystem ps, float shaftWidth)
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.zero;
            shape.scale = new Vector3(shaftWidth, BoxHeight, 0.5f);
        }

        private static void ConfigureVelocityOverLifetime(ParticleSystem ps)
        {
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            // x/y/z must share one curve mode (Shuriken rejects a per-axis mode mismatch), so x/z
            // get an explicit zero TwoConstants curve instead of being left at their differently-
            // moded default.
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        private static void ConfigureNoise(ParticleSystem ps)
        {
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(1.2f);
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.8f;
            noise.damping = true;
        }

        private static void ConfigureSizeOverLifetime(ParticleSystem ps)
        {
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.4f, 1f),
                new Keyframe(1f, 0.1f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void ConfigureColorOverLifetime(ParticleSystem ps)
        {
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.black, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;
        }

        private static void ConfigureRenderer(GameObject root, GameObject flameGo)
        {
            var renderer = flameGo.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) renderer = flameGo.AddComponent<ParticleSystemRenderer>();

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

            Transform surface = root.transform.Find("Surface");
            var surfaceRenderer = surface != null ? surface.GetComponent<SpriteRenderer>() : null;
            if (surfaceRenderer != null)
            {
                renderer.sortingLayerID = surfaceRenderer.sortingLayerID;
                renderer.sortingOrder = surfaceRenderer.sortingOrder + 1;
            }
        }

        // FogFlameEdge.shader/.mat targeted sprite-UV space and is no longer the active effect
        // (see class doc comment) — left in the repo unused rather than deleted, in case the
        // outline-ridge look is revisited later. Surface goes back to Unity's default sprite
        // material, same reference FogSystem.prefab shipped with before that shader was assigned.
        private static void RevertSurfaceMaterial(GameObject root)
        {
            Transform surface = root.transform.Find("Surface");
            var surfaceRenderer = surface != null ? surface.GetComponent<SpriteRenderer>() : null;
            if (surfaceRenderer == null)
            {
                Debug.LogWarning("[FogFlameParticleBuilder] No 'Surface' SpriteRenderer to revert.");
                return;
            }

            surfaceRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        }
    }
}
