# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 2D vertical climber. The player latches to mirrored walls in a shaft, slides down under
gravity, taps to wall-jump across, and dashes to grapple nodes to gain height, while a lethal fog
wall rises from below. Height climbed is the score.

- Unity **6000.3.10f1**, Universal Render Pipeline 17.3.0
- DI and messaging: **VContainer 1.19.0**, **MessagePipe 1.8.2** (plus MessagePipe.VContainer),
  **UniTask 2.5.11**
- Also: new Input System 1.18.0, Addressables, Localization, TextMeshPro, Timeline, DOTween,
  Gabriel Bigardi 2D Sprite Animator

## Commands

The `unity` CLI is installed and drives the editor from this directory.

```bash
unity test --mode EditMode                 # EditMode suite
unity test --mode PlayMode                 # PlayMode suite (loads real scenes)
unity test --mode EditMode --filter HitstopTests             # one fixture
unity test --mode EditMode --filter "*Pause_FreezesTime*"    # one test
unity open .                               # open in the editor
unity build --target StandaloneWindows64 -o Build/game.exe
unity status                               # editors currently connected
```

Results are written to `test-results.xml` (NUnit) unless `--output` overrides it. Batch mode cannot
attach to a project an open editor holds; when an editor is already running, use the **unity-mcp**
MCP server instead (console logs, scene capture, run command).

Editor menu **NestLabs > Debug > Build Obstacle Debug Scene** regenerates
`Assets/Scenes/obstacle-debug.unity` from every obstacle prefab. Headless equivalent:
`-executeMethod NestLabs.EditorTools.DebugObstacleSceneBuilder.Build`.

### Scenes

- `test-merge.unity` is the main working scene, the one actually played during development.
- `dev-awe.unity` is the first Build Settings entry and the only enabled gameplay scene. Keep it
  wired in step with `test-merge`, they are meant to match.
- Both of the above carry a `GameLifetimeScope` and a `HUD` canvas holding a `GUI.prefab` instance.
- `YaserScene.unity` is loaded by `LevelGeneratorSpawnTests` by hardcoded path. Renaming it or
  unwiring its rule assets breaks the PlayMode suite. It has no HUD on purpose.
- `obstacle-debug.unity` is generated, do not hand-edit it.
- `MenuScene.unity` is a sandbox.

## Assemblies

Code is split into asmdefs with a one-directional reference graph. A `using` that reverses an arrow
will not compile.

```
NestLabs.Shared        (no references, leaf: interfaces, enums, event structs, null objects)
  <- Nestlabs.Level    (spawn rule contracts, LevelGenerator, pooling)
       <- Nestlabs.Obstacle, NestLabs.Node, Nestlabs.Wall
  <- NestLabs.Runtime  (Assets/Scripts root: player, audio, score, flow, DI, UI, hazards)
Nestlabs.Environment   (isolated, parallax only)
NestLabs.EditorTools   (Editor-only)
NestLabs.Tests.EditMode / NestLabs.Tests.PlayMode
```

`NestLabs.Runtime` does **not** reference `Nestlabs.Wall` or `Nestlabs.Environment`. Anything the
player or a service must see belongs in `NestLabs.Shared`.

Namespace casing is inconsistent and follows the asmdef that owns the file: `NestLabs.*` for Player,
Audio, Score, Node, Shared, UI, EditorTools, but `Nestlabs.*` (lowercase L) for Level, Obstacle,
Wall, Environment. Match the folder you are in rather than normalizing.

`AssemblyInfo.cs` in `Assets/Scripts/` and `Assets/Scripts/Node/` grants `InternalsVisibleTo` to
`NestLabs.Tests.EditMode`. That is how tests stage `PlayerSensor.Current` and `NodeBase.Data`
without real colliders or asset wiring.

## Architecture

### Composition root

`Assets/Scripts/DI/GameLifetimeScope.cs` is the single VContainer `LifetimeScope`. It registers every
MessagePipe broker, the event sinks, input, config and library assets, and resolves components in the
hierarchy. Two non-MonoBehaviour singletons, `AudioEventBinder` and `IGameStateService`, are
force-resolved in `RegisterBuildCallback` because nothing else pulls on them and their constructors
are where the subscriptions happen.

Injection is method injection (`[Inject] public void Construct(...)`). VContainer runs it during the
scene's Awake pass with **no ordering guarantee against a component's own Awake**. The established
pattern is: `Construct` only stores references, real assembly happens in `Start`
(`PlayerBase.Build`) or lazily in `Update` (`LevelGenerator`). Follow it, do not read an injected
field from `Awake`.

### Events

Domain code never touches MessagePipe directly. It publishes through a narrow sink interface in
`NestLabs.Shared`:

- `IPlayerEventSink` implemented by `MessagePipePlayerEventSink` (or `NullPlayerEventSink`)
- `IObstacleEventSink` implemented by `MessagePipeObstacleEventSink` (or `NullObstacleEventSink`)

Subscribers use MessagePipe directly. `AudioEventBinder` is the only place domain events map to SFX
and music, `ScoreService` and `ScoreHud` subscribe for score, `GameStateService` subscribes to
`PlayerDiedEvent`. Adding an SFX trigger should touch only `AudioEventBinder`.

Null-object implementations (`NullPlayerEventSink`, `NullHitstop`, `NullGameStateService`,
`NullPlayerInput`, `NullObstacleEventSink`) exist so a prefab dropped into a bare scene with no
container still runs, degraded to "publishes nothing" instead of null-referencing. Consumers fall
back to them with `??=`. `NullHitstop.Safe(x)` additionally guards against a destroyed MonoBehaviour
service.

### Flow state

`IGameStateService` / `GameStateService` owns `Menu -> Play -> Pause / Death` with an explicit
allowed-transition table; a rejected transition logs a warning and does nothing. The scene boots into
`Menu` (player standing on the floor) and the first tap starts the run. Systems gate on
`IsPlaying` rather than inventing their own paused flag, because `Update` keeps running through
Death and Pause.

### Player

`Assets/Scripts/Player/`. `PlayerBase` is the only class that wires components together, which is
what keeps `GetComponent` out of every state. It builds one `PlayerContext` holding every
collaborator plus a small blackboard (active node, grapple decay, facing, coyote-wall bookkeeping)
and hands it to each state.

`PlayerStateMachine` indexes states by the dense `PlayerStateId` enum (array, not dictionary). States
are plain C# objects allocated once at startup, so transitions never allocate. `ChangeState` is
re-entrant safe: a change requested from inside `Enter`/`Exit` is queued and drained by the outermost
call. States: Idle, Latch, Slide, Jump, Fall, Dash, Hit, Dead.

Input is a buffered tap. `PlayerStateMachine.Tick` re-offers a buffered tap every frame until some
state consumes it, which is what makes a tap fired mid-dash fire again when the dash ends. States
implement `OnTap` and consume only if they act on it.

`PlayerSensor.Probe()` runs once per `FixedUpdate` from `PlayerBase` and publishes an immutable
`PlayerSense` struct. States read `Context.Sense`, they never cast rays. Walls are detected purely by
a non-trigger `Collider2D` on the solid layer, with no tag or component check.

Damage flows `PlayerHurtbox` to `IDamageSource` to `PlayerHealth.TryApplyDamage` (which owns the
i-frame decision) to `IHittable.OnHit` on the source, only for accepted hits, so re-contact during
i-frames cannot re-fire an obstacle's SFX.

All tuning lives in `PlayerConfigSO` (`Assets/ScriptableObjects/Player/PlayerConfig_Default.asset`).

### Level generation

`Nestlabs.Level.LevelGenerator` is the only `Update` loop for spawning. What spawns and how is
entirely defined by the `SpawnRuleSO` assets in its `rules` list, so a new prefab variant or a
cadence retune is a data change, not a code change. Rule assets live in
`Assets/ScriptableObjects/*/Rules/`.

Three things about this system are load-bearing:

1. **Rules are cloned.** `LevelGenerator.Awake` calls `Instantiate` on each `SpawnRuleSO` so timer
   and progression state never leaks back into the shared project asset across play sessions.
2. **Everything pools.** `SpawnRuleContext.Spawn`/`Despawn` wrap `ObjectPool<Component>`, one pool
   per distinct prefab reference. Do not `Instantiate`/`Destroy` obstacles, walls, or nodes
   directly. Components needing `Start`-style setup on reuse implement `IPoolable`, since Unity does
   not re-run `Start` on a reactivated instance.
3. **Physics sync is batched.** Spawning sets `ctx.TransformsDirty`; `LevelGenerator` flushes one
   `Physics2D.SyncTransforms()` per frame after all rules tick. Without it a same-frame cast sees a
   reused collider at its stale position.

`ISpawnRule` has `Initialize` / `Prime` / `Tick`. `Prime` fills the opening layout once before the
run starts so the player can read it during the ready pose; steady-state spawning only happens in
`Tick`, and only while `IsPlaying`. `SpawnRuleContext.AddClaim` / `ClaimClearance` let grapple nodes
reserve their grab radius so obstacles never spawn inside a point the player must reach.

Rule types: `DistanceSpawnRuleSO`, `IntervalSpawnRuleSO`, `WeightedGroupSpawnRuleSO`,
`ProjectileSpawnRuleSO`, `NodeSpawnRuleSO`, `WallPairSpawnRuleSO`.

### Other systems

- **Obstacles** (`Nestlabs.Obstacle`): `ObstacleBase` implements `IHittable` and `IDamageSource`, so
  the hurtbox finds any obstacle without knowing its type. Variants: Idle, Moving, Swing, Projectile.
- **Nodes** (`NestLabs.Node`): `NodeBase` is a grapple point with no player knowledge. Tuning comes
  from `NodeDataSO`, so variants differ by asset, not script.
- **Fog** (`FogSystem`): an `IDamageSource` with damage far above max health, so the existing hurtbox
  pipeline handles the kill with no player-side change.
- **Hitstop**: lives on a services object, not the player, so a player destroyed mid-dip cannot leave
  the game slowed. Resets `Time.timeScale` on `RuntimeInitializeOnLoadMethod`.
- **Audio**: `AudioService` (`IAudioService`) plus `SfxAudioSourcePool`, keyed by the `SfxId` /
  `MusicId` enums so lookups index an array instead of hashing strings.
- **Score**: `ScoreService` computes points per world unit climbed, `IScoreStore` persists the best
  via PlayerPrefs, `ScoreHud` is pure display driven only by events.
- **UI**: all panels live in `Assets/Prefabs/UI/GUI.prefab`, which nests `Pause`, `Credits` and
  `Died` under an `Overlay` child. `HudPanelController` is the only script that maps flow state to
  panel visibility and routes the buttons back into `IGameStateService`; the panels themselves stay
  script-free. It also gates `IPlayerInput.Enabled` so a tap on a panel is not also spent as a wall
  jump. `Credits` is a sub-view of `Pause`, not a `GameState`. Menu item
  **NestLabs > Debug > Wire HUD Panels** rebuilds the prefab wiring and both scene instances.

## Conventions

- New `ScriptableObject` types get `[CreateAssetMenu]` under the `NestLabs/` menu root.
- Comments explain why, not what, and non-obvious decisions carry the reasoning inline. Match that
  density; several classes exist only because of a specific Unity ordering or pooling hazard, and
  deleting the note loses the reason.
- Enter Play Mode Options are enabled (`m_EnterPlayModeOptionsEnabled: 1`), so verify any assumption
  about static state or domain reload against that setting.
- Serialized field naming is mixed (`_camelCase` in Player/Audio/Node, bare `camelCase` in
  Level/Score). Match the file being edited.
- Generated `.csproj` files sit at the repo root and are gitignored; the `.slnx` solution is tracked.
