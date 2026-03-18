# Core Battle

## TL;DR
- Purpose: Runs battle matches through `Game` using intent-to-command routing and rule-based completion.
- Location: `Assets/Scripts/Core/Battle/Runtime/`.
- Depends on: `Madbox.Enemies`, `Madbox.Levels`, `Madbox.Gold`, `Scaffold.MVVM.Model`, `Scaffold.Records`.
- Used by: battle presentation/bootstrapping flows and test assembly `Madbox.Battle.Tests`.
- Runtime/Editor: pure runtime C# module with EditMode tests.
- Keywords: battle, game loop, intents, commands, rules, completion.

## Responsibilities
- Owns: battle lifecycle (`Game.Start`, `Game.Tick`, `Game.Trigger`), event routing, player runtime state, and game-end orchestration.
- Owns: reward payout on win through `GoldWallet`.
- Does not own: enemy instantiation/state internals (owned by `Madbox.Enemies`).
- Does not own: level authoring/data schemas (owned by `Madbox.Levels`).
- Does not own: Unity movement/collision simulation or line-of-sight checks.
- Boundaries: pure C# runtime only (`noEngineReferences: true`), no `MonoBehaviour`.

## Public API
| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `Game` | Runs a single battle instance | `LevelDefinition`, `GoldWallet`, `Player` in ctor; `Start/Tick/Trigger` calls | Emits `EventTriggered` and `OnCompleted` | Ctor throws on nulls/invalid level id; ignores tick/trigger when not running |
| `Player` | Player runtime model used by battle commands and rules | `EntityId`, initial health | Mutable health and behavior runtime list | Ctor guards invalid args; damage methods clamp/ignore invalid values |
| `GameState` | Exposes lifecycle state | N/A | `NotRunning`, `Running`, `Done` | N/A |
| `BattleEvent` records (`TryPlayerAttack`, `EnemyHitObserved`, `PlayerAutoAttackObserved`, `PlayerProjectileHitObserved`, `PlayerAttack`, `PlayerProjectileSpawned`, `EnemyKilled`, `PlayerDamaged`, `PlayerKilled`) | Input intents and output domain events | Record fields per event type | Immutable event payloads | Router ignores unknown/unroutable events |

## Setup / Integration
1. Add asmdef dependency to `Madbox.Battle` and its required modules (`Madbox.Enemies`, `Madbox.Levels`, `Madbox.Gold`).
2. Construct `Game` with a level, wallet, and player model instance.
3. Subscribe to `EventTriggered` and `OnCompleted` before calling `Start()`.
4. Feed frame updates through `Tick(deltaTime)` and inputs/observations via `Trigger(BattleEvent)`.
5. Validate setup quickly by running `validate-changes.cmd` and confirming `TOTAL:0` analyzers.
- Common mistake: triggering events before `Start()`; they are ignored by design.
- Common mistake: expecting enemy attack distance checks in core battle; those are presentation-driven.

## How to Use
1. Build a `LevelDefinition` and `Player` for the match.
2. Create `Game` and subscribe to output callbacks.
3. Call `Start()` once.
4. On each frame, call `Tick(deltaTime)`.
5. Route player/view intents and hit observations into `Trigger(...)`.
6. End flow from `OnCompleted(GameEndReason)` and apply UI transitions outside core.

## Examples
### Minimal
```csharp
LevelDefinition level = BuildLevel();
GoldWallet wallet = new GoldWallet();
Player player = new Player(new EntityId("player-1"), 100);
Game game = new Game(level, wallet, player);

game.Start();
game.Tick(0.016f);
```

### Realistic
```csharp
game.EventTriggered += evt => HandleBattleEvent(evt);
game.OnCompleted += reason => HandleCompletion(reason);

game.Start();
game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
game.Trigger(new EnemyHitObserved(new EntityId("enemy-1"), new EntityId("player-1"), 5));
game.Tick(0.016f);
```

### Guard / Error path
```csharp
Game game = new Game(level, wallet, player);

game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1"))); // ignored before Start

game.Start();
game.Tick(0f); // ignored (non-positive delta)
```

## Best Practices
- Keep battle inputs as intents/observations; avoid direct state mutation from UI.
- Keep commands self-contained and mutate domain models directly.
- Let level-defined rules drive completion instead of hardcoded end checks.
- Keep `Game` orchestrator-focused; push specific concerns to internal services/router.
- Preserve pure C# boundaries and keep Unity-facing concerns out of this module.
- Maintain analyzer compliance (avoid suppressions unless strictly necessary).

## Anti-Patterns
- Placing movement or line-of-sight simulation inside `Game` or battle services.
- Adding reset/restart semantics to a `Game` instance instead of constructing a new one.
- Routing event logic through large `switch` blocks in `Game` instead of the router.
- Mutating enemy internals directly from battle presentation code.
- Migration guidance: move enemy-specific operations to `Madbox.Enemies`; keep battle module orchestration-only.

## Testing
- Test assembly: `Assets/Scripts/Core/Battle/Tests/Madbox.Battle.Tests.asmdef` (`Madbox.Battle.Tests`).
- Run:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-editmode-tests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\run-playmode-tests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\.agents\scripts\check-analyzers.ps1"
```
- Expected: all tests pass, no failures, analyzer `TOTAL:0`.
- Bugfix rule: add/update a regression test that fails before the fix and passes after it.

## AI Agent Context
- Invariants:
  - `Game` is a single battle instance; create a new `Game` instead of resetting.
  - Completion is determined by level rule definitions evaluated in `GameRuleEvaluator`.
  - Enemy attack resolution remains presentation-driven through observed hit events.
- Allowed Dependencies:
  - `Madbox.Enemies`
  - `Madbox.Levels`
  - `Madbox.Gold`
  - `Scaffold.MVVM.Model`
  - `Scaffold.Records`
- Forbidden Dependencies:
  - UnityEngine/MonoBehaviour types in runtime code.
  - Direct references to presentation/view assemblies.
- Change Checklist:
  - Keep `Madbox.Battle.asmdef` dependency list explicit and minimal.
  - Keep analyzer warnings at `TOTAL:0`.
  - Run EditMode + PlayMode + analyzer scripts.
  - Ensure new behaviors/events remain routed through the battle router.
- Known Tricky Areas:
  - Intent vs observed-event separation for attacks/projectiles.
  - Win reward must only apply on `GameEndReason.Win`.
  - Namespace/folder alignment required by analyzers.

## Related
- `Architecture.md`
- `Docs/Testing.md`
- `Docs/AutomatedTesting.md`
- `Docs/Core/Levels.md`
- `Docs/Core/Enemies.md`

## Changelog
- 2026-03-18: Rewrote module doc to match Module Documentation Standard section order and constraints.
- 2026-03-18: Updated content for intent-command pipeline, level-defined rules, and Enemies module extraction.
