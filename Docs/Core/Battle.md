# Core Battle

## Purpose

`Madbox.Battle` provides the battle domain runtime (`Game`) with a single input endpoint (`Trigger`) and a single output callback (`EventTriggered`), plus `OnCompleted(GameEndReason)` for match completion.

## Public API

- Enums: `GameState`, `GameEndReason`
- Engine: `Game`
- Events:
  - Inputs: `TryPlayerAttack`, `EnemyHitObserved`
  - Outputs: `PlayerAttack`, `EnemyKilled`, `PlayerDamaged`, `PlayerKilled`

## Usage Example

```csharp
Game game = new Game(levelDefinition, goldWallet, new EntityId("player-1"));
game.EventTriggered += OnBattleEvent;
game.OnCompleted += OnCompleted;

game.Start();
game.Tick(0.016f);

game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
game.Trigger(new EnemyHitObserved(new EntityId("enemy-1"), new EntityId("player-1"), rawDamage: 5));
```

## Design Notes

- Movement and attack-intent decisions stay on prefab/view side.
- Collision/hit is the enemy-side input that enters game logic via `EnemyHitObserved`.
- `Game` orchestrates specialized internal systems:
  - `EnemyService` for enemy behavior/runtime mutations.
  - `BattleEventRouter` that routes event types through internal methods using `EnemyService` + `Player`.
  - `GameRuleEvaluator` that builds and evaluates rules from `LevelDefinition` to detect end state during tick.
- `Game` only exposes high-level game state (`CurrentState`, `ElapsedTimeSeconds`, `CurrentLevelId`) and does not use `BattleRuntimeState`.
