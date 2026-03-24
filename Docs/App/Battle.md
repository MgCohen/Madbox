# Battle (App)

## TL;DR

- Purpose: Unity-native battle slice from `LevelDefinition`: Addressables scene load, prefab enemies via `EnemyService`, and rule evaluation through `RuleHandlerRegistry`.
- Location: `Assets/Scripts/App/Battle/Runtime/` (`Madbox.Battle`), tests in `Assets/Scripts/App/Battle/Tests/` (`Madbox.Battle.Tests`).
- Depends on: `Madbox.Enemies`, `Madbox.Levels`, `Madbox.SceneFlow`, Addressables, VContainer (via installers).
- Used by: `Madbox.Gameplay` (`GameSessionCoordinator`, `BattleGameFactory`), bootstrap `BattleGameplayInstaller`.
- Runtime/Editor: runtime only.

Keywords: battle, rules, enemies, session, factory

## Responsibilities

- Owns: `BattleGame` session lifecycle, `BattleGameFactory` (prepare/start after additive load), `RuleHandlerRegistry`, enemy spawn and rule evaluation for a level.
- Does not own: level authoring assets (`Madbox.Levels`), navigation/UI, or LiveOps progression (`Madbox.Level`).
- Boundaries: Unity runtime; level data stays data-only; rule logic lives in handlers registered against rule types, not on ScriptableObjects.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `BattleGame` | Mutable battle session: spawn enemies, tick time, evaluate rules, raise completion. | `Tick`, factory wiring | Session state, `OnCompleted` | Undefined if not started per factory contract. |
| `BattleGameFactory` | Builds `BattleGame`, loads/spawns enemies, session world root, player spawn, Cinemachine follow when configured. | `CreatePrepareStartAsync` / `CreatePrepareStartAfterAdditiveSceneLoadAsync`, `LevelDefinition` | Running `BattleGame` | Throws or fails async on missing assets/scene contract. |
| `RuleHandlerRegistry` | Maps `LevelRuleDefinition` asset types to `RuleHandler<TRule>`. | Register handlers, evaluate | Rule outcomes | Unregistered rule types skip or error per handler design. |

## Setup / Integration

1. Register rule handlers on `RuleHandlerRegistry` (for example `TimeElapsedCompleteRule` → `TimeElapsedCompleteRuleHandler`).
2. Register `EnemyService` (transient) and inject `Func<EnemyService>` plus `RuleHandlerRegistry` into `BattleGameFactory` when using `CreatePrepareStartAfterAdditiveSceneLoadAsync` (see `BattleGameplayInstaller`).
3. After additive level load via `ISceneFlowService`, call `BattleGameFactory.CreatePrepareStartAfterAdditiveSceneLoadAsync` with `SceneFlowLoadResult` and `LevelDefinition`.
4. Ensure `GameSessionCoordinator` only loads/unloads the level scene; `BattleGameFactory` owns battle and session world setup.

## How to Use

1. Wire `BattleGameplayInstaller` from bootstrap (already done in sample).
2. Register each `RuleHandler<TRule>` against the registry before starting a session.
3. After scene flow reports a successful additive load, call the factory overload that accepts the load result.
4. Tick `BattleGame` each frame while the session runs.
5. Subscribe to `BattleGame` completion to drive win/lose UI (see `Madbox.Gameplay`).

## Examples

### Minimal

```csharp
// Handlers registered on RuleHandlerRegistry before CreatePrepareStartAsync
registry.Register<TimeElapsedCompleteRule, TimeElapsedCompleteRuleHandler>();
var battle = await factory.CreatePrepareStartAfterAdditiveSceneLoadAsync(loadResult, levelDefinition, ct);
```

### Realistic

```csharp
// After ISceneFlowService loads the level additively
var battle = await battleGameFactory.CreatePrepareStartAfterAdditiveSceneLoadAsync(
    sceneFlowResult,
    selectedLevel,
    cancellationToken);
while (!battle.IsCompleted)
{
    battle.Tick(Time.deltaTime);
    await Task.Yield();
}
```

### Guard / Error path

```csharp
// Missing handler for a rule type: registration must cover every rule asset type on LevelDefinition
// Fix: register handler before session start; keep level assets data-only
```

## Best Practices

- Keep level assets data-only; implement rules in `RuleHandler` types.
- Register all rule types used by `LevelDefinition` before creating `BattleGame`.
- Use `EnemyService.Tick` each frame while battle is active so delayed destroys complete after death presentation.
- Keep spawn bounds logic in factory/enemy code; prefer arena bounds from `Arena` when available.

## Anti-Patterns

- Embedding rule logic on `ScriptableObject` level assets — use handlers instead.
- Skipping `BattleGameFactory` after additive load — session world and player spawn wiring lives there.
- Duplicating Addressables resolution outside the factory for enemies — keep load paths consistent with `LevelDefinition`.

## Testing

- Test assembly: `Madbox.Battle.Tests` (EditMode).
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Battle.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first; confirm fail-before, pass-after.

## AI Agent Context

- Invariants: `LevelDefinition` remains data-only; handlers own rule semantics; `GameSessionCoordinator` does not own battle logic beyond load/unload.
- Allowed Dependencies: `Madbox.Enemies`, `Madbox.Levels`, `Madbox.SceneFlow`, Addressables, infra registered by bootstrap.
- Forbidden Dependencies: App UI assemblies, MainMenu, direct LiveOps client types in battle core.
- Change Checklist: register new rule types in `RuleHandlerRegistry`; update tests for spawn/rule paths; run `Madbox.Battle.Tests`.
- Known Tricky Areas: enemy spawn bounds vs arena `BoxCollider`; `EnemyService.Tick` must run every frame during battle.

## Related

- `Docs/App/Gameplay.md`
- `Docs/Meta/Levels.md`
- `Docs/Meta/Enemies.md`
- `Docs/Infra/SceneFlow.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Restructured to `Module-Documentation-Standard.md` (full section order and tables).
