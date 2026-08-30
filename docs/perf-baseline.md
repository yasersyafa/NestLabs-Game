# Performance Baseline

Captured **2026-08-30**, branch `developer/yaser`, commit `b50ca09` (+ Fase 0 instrumentation),
before any optimisation work. Re-run the exact steps below after each change and compare.

## How to reproduce

### Runtime (spawn loop) — PlayMode perf test

```bash
unity test --mode PlayMode --filter LevelGeneratorPerfTests --output baseline/playmode-perf.xml
```

`Assets/Tests/PlayMode/LevelGeneratorPerfTests.cs` — synthetic climber ascends `YaserScene.unity`
for 2000 measured frames (120 warmup) through the real DI + rule assets + pooling. Records frame
time and `ProfilerMarker` groups. Open **Window > Analysis > Performance Test Report** in-editor,
or diff the XML.

Markers live in code (kept permanently, alloc-free): `LevelGenerator.TickRules`,
`LevelGenerator.SyncTransforms`, `PlayerSensor.Probe`, `PlayerMotor.Move`,
`ScoreService.RecomputeScore`, `ParallaxController.UpdateLayers`. In this synthetic test only the
`LevelGenerator.*` markers get samples (real player is idle, not climbing).

### Build size — Standalone

```bash
unity build --target StandaloneOSX -o Build/baseline/game.app
du -sh Build/baseline/game.app
```

### Deeper captures (manual, in editor)

- **Profiler**: play `dev-awe.unity`, climb ~30 s, capture, save `.data` to `baseline/`.
- **Memory Profiler** (`com.unity.memoryprofiler`): snapshot after ~30 s climb in `dev-awe.unity`,
  save to `baseline/`. Look for retained garbage + duplicated textures.
- **Profile Analyzer** (`com.unity.performance.profile-analyzer`): load the baseline `.data` as
  "Left", the post-change capture as "Right", compare median frame + marker times.

## Baseline numbers

### Runtime — `LevelGeneratorPerfTests.SteadyStateSpawnLoop_FrameCost`

Editor PlayMode, 2000 frames, climbed to y≈1060. Active at end: 5 obstacles, 4 walls, 7 nodes
(column is bounding itself — good).

| Sample group | Min | Median | Avg | Max | StdDev |
|---|---|---|---|---|---|
| Frame time | 1.11 ms | **1.69 ms** | 1.99 ms | 91.95 ms | 2.31 ms |
| `LevelGenerator.TickRules` | 7.5 µs | **15.8 µs** | 41.9 µs | 4.9 ms | 121 µs |
| `LevelGenerator.SyncTransforms` | 0 | **0** | 25.4 µs | 1.0 ms | 55 µs |

Notes:
- Editor overhead dominates; `Max` spikes are editor GC / domain hitches, not gameplay.
  `Median` is the number to track.
- The spawn loop itself is already cheap (`TickRules` median ~16 µs, `SyncTransforms` fires only
  on spawn/cull frames). Matches the code review: `Assets/Scripts` runtime is alloc-free and
  tight. Runtime wins in the plan are small/localised (ScoreService HUD throttle, debug HUD
  strip); the large wins are in **build size / GPU config**, not CPU frame cost.
- This test is an in-editor proxy. Real device frametime must be measured with a Development
  Build + on-device Profiler once a signing/toolchain path exists.

### Build size — Standalone (StandaloneOSX)

`unity build --target StandaloneOSX -o Build/baseline/game.app` (Mono, no managed stripping).

| Item | Size |
|---|---|
| `game.app` total | **142 MB** |
| `Contents/Frameworks` (UnityPlayer runtime) | 69 MB |
| `Contents/Resources` (Data) | 69 MB |
| &nbsp;&nbsp;`Data/Managed` (unstripped managed DLLs) | 36 MB |
| &nbsp;&nbsp;`Data/sharedassets0.assets.resS` (scene-0 textures + TMP font atlases) | 14 MB |
| &nbsp;&nbsp;`Data/globalgamemanagers.assets.resS` (built-in / URP resources) | 5.5 MB |
| &nbsp;&nbsp;`Data/sharedassets0.resource` (music, Vorbis on disk) | 3.2 MB |
| Build time | ~9.5 min (cold) |

## After Fase 1 (2026-08-30)

`unity build --target StandaloneOSX -o Build/after/game.app`. Same command, same tree + the
Fase 1 changes (music import, 6 package removals, post-fx variant strip, managed stripping Low,
il2cpp OptimizeSize, `link.xml`).

| Item | Baseline | After | Δ |
|---|---|---|---|
| `game.app` total | 142 MB | **121 MB** | **−21 MB (−15%)** |
| `Data/Managed` | 36 MB | **19 MB** | −17 MB (managed stripping Low works on Mono too) |
| `Data/sharedassets0.resource` (music) | 3.2 MB | **448 KB** | −2.8 MB (mono + q0.4 + streaming) |
| `Contents/Frameworks` (UnityPlayer) | 69 MB | 69 MB | 0 (fixed engine runtime) |
| `Data/sharedassets0.assets.resS` | 14 MB | 14 MB | **0 — byte-identical, untouched** |

Tests after Fase 1: EditMode **44** green (was 45 — the vanished one was
`AddressableAssets.DocExampleCode.TestStub.RequiredTest`, a stub shipped inside the Addressables
package; removing the package removed its stub, not a real test). PlayMode **2** green. Perf test
median unchanged.

### Still open — `sharedassets0.assets.resS` 14 MB

Unaffected by Fase 1 and byte-identical between builds. Contains `Font Atlas` objects (TMP SDF)
plus scene-0 sprite textures. This is the next build-size target:
- TMP font atlases (`LiberationSans SDF` 1024², `FetteNationalFraktur SDF`, `IMFellEnglish SDF`)
  — trim to used glyph set, drop the unused fallback.
- Scene-0 (`dev-awe`) sprite textures — the deferred **Fase 1.3** (crunch + Android ASTC
  override) belongs here; do it as a `TextureImporter` batch script, not 24 hand-edits.
- `SpriteAtlas` for `Assets/Art/Sprites/**` — also cuts draw calls (Fase 3 GPU work).

### Deferred from Fase 1
- **`_Dummy/`** not deleted — `ParallaxBackground.prefab` and `FogSystem.prefab` use art from it
  as placeholders. Replace with final art, then trim.
- **1.3 texture crunch** — deferred to the `sharedassets0.assets.resS` pass above.
- **Splash screen / companyName / applicationIdentifier** — publishing decisions, left to owner.

## After Fase 2 (2026-08-31) — runtime frametime

Code changes, no data/build-setting changes:
- **`ScoreService.RecomputeScore`** — was `Publish(ScoreChangedEvent)` on every frame the player
  gained any height (→ `string.Format` ×2 + TMP mesh rebuild ×2 in `ScoreHud` per frame). Now
  publishes only when the rounded `int` score or best actually changes — roughly once per world
  unit climbed instead of once per frame.
- **`PlayerDebugHud`** — `OnGUI` (per-frame IMGUI, interpolated strings, `new Rect`/`GUIStyle`)
  and the R-to-restart `Update` are now `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. A release build
  has no on-screen debug readout and pays nothing.
- **`ProjectileObstacle`** — `Camera.main` (tagged-object scan) was called every frame while a
  warning icon is on screen; cached in `Awake`, refreshed only if null.
- **`SpawnRuleContext`** pools — added `defaultCapacity: 8, maxSize: 32` so a spawn spike can no
  longer retain unbounded inactive instances for the session.

Tests: EditMode 44 green, PlayMode 2 green. Perf test (in-editor, synthetic climber — does not
exercise the real `ScoreService`/`PlayerBase`, so treat as a no-regression check): frame median
1.69 ms → 1.17 ms, `LevelGenerator.TickRules` 15.8 µs → 10.3 µs (within editor variance). Build
size unchanged (release build already stripped `OnGUI`; see table).

### Skipped in Fase 2 (with reason)
- **`ParallaxLayer.UpdatePosition`** world-space writes — baseline shows parallax is not a hot
  path, and the class's drift-free property (documented in `CLAUDE.md`) is not worth risking for
  ~10 transform writes/frame.
- **`PlayerHurtbox.OnTriggerStay2D`** `GetComponentInParent<IDamageSource>` per physics step —
  only while overlapping fog/hazard, and it sits in the i-frame damage pipeline. Not worth the
  risk for the saving.

## Regression policy

Any change in Fase 1–3 of `~/.claude/plans/kalau-mau-optimisasi-project-quiet-donut.md`:
1. `unity test --mode EditMode` — 44 tests, must stay green.
2. `unity test --mode PlayMode` — 2 tests, must stay green (`MaxReasonableWalls` / wall guard).
3. Re-run the perf test; `Median` frame + marker times must not regress.
4. Re-run the build; record new size in the table above.
