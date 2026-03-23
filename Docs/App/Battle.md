# Battle (App)

## Purpose

Provides a Unity-native battle slice that runs from `LevelDefinition`: Addressables scene load, prefab-based enemies via `EnemyService`, and rule evaluation through `RuleHandlerRegistry`.

- Location: `Assets/Scripts/App/Battle/Runtime/` (`Madbox.Battle`), tests in `Assets/Scripts/App/Battle/Tests/`.

## Public API

| Type | Role |
|------|------|
| `BattleGame` | Mutable battle session: spawn enemies, tick time, evaluate rules, raise `OnCompleted`. |
| `BattleGameFactory` | Constructs `BattleGame` and loads/spawns enemies from level `AssetReferenceT<EnemyData>` entries. |
| `BattleBootstrap` | One-call orchestration: load level scene, create `BattleGame`, spawn enemies, `Start()`. |
| `BattleBootstrapResult` | Holds `Game` and the Addressables `SceneInstance` load handle (caller must release when done). |
| `RuleHandlerRegistry` | Maps `LevelRuleDefinition` asset types to `RuleHandler<TRule>` implementations. |

## Usage

1. Register rule handlers on a `RuleHandlerRegistry` (for example `TimeElapsedCompleteRule` → `TimeElapsedCompleteRuleHandler`).
2. Create `EnemyService` with `EnemyFactory`.
3. Call `BattleBootstrap.StartBattleAsync(level, enemyService, registry, spawnOrigin, spacing)` and await completion.
4. Tick `result.Game` each frame while running; release `result.SceneLoadHandle` when tearing down the battle.

## Design notes

- Level assets remain data-only; rule logic lives in handlers, not on ScriptableObjects.
- Enemy loads use `Addressables` directly on serialized references; there is no separate resolver type in this assembly.
