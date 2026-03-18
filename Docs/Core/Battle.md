# Core Battle

## Purpose

`Madbox.Battle` provides the battle domain runtime (`Game`) with a single input endpoint (`Trigger`) and a single output callback (`EventTriggered`), plus `OnCompleted(GameEndReason)` for match completion.

## Public API

- Enums: `GameState`, `GameEndReason`
- Runtime model: `BattleRuntimeState` (observable state for HP, counters, elapsed time)
- Engine: `Game`
- Events:
  - Inputs: `TryPlayerAttack`, `EnemyHitObserved`
  - Outputs: `PlayerAttack`, `EnemyKilled`, `PlayerDamaged`, `PlayerKilled`

## Usage Example

```csharp
Game game = new Game(levelDefinition, goldWallet, new EntityId("player-1"));
game.EventTriggered += OnBattleEvent;
game.OnCompleted += OnCompleted;

game.Initialize();
game.Start();
game.Tick(0.016f);

game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
game.Trigger(new EnemyHitObserved(new EntityId("enemy-1"), new EntityId("player-1"), rawDamage: 5));
```

## Design Notes

- Movement and attack-intent decisions stay on prefab/view side.
- Collision/hit is the enemy-side input that enters game logic via `EnemyHitObserved`.
- `Game` validates and applies authoritative state updates, then emits transient output events.
- Permanent state changes (HP, counts, elapsed time) are reflected through `BattleRuntimeState`.
