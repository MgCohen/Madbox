# Core Levels

## Purpose

`Madbox.Levels` defines level content models used by the battle runtime: IDs, enemy definitions, behavior definitions, and constructor-level invariant checks.

## Public API

- Value records: `LevelId`, `EntityId`
- Behavior types: `EnemyBehaviorDefinition`, `MovementBehaviorDefinition`, `ContactAttackBehaviorDefinition`
- Models: `EnemyDefinition`, `LevelEnemyDefinition`, `LevelDefinition`

## Usage Example

```csharp
EnemyDefinition slime = new EnemyDefinition(
    new EntityId("slime"),
    maxHealth: 20,
    new EnemyBehaviorDefinition[]
    {
        new MovementBehaviorDefinition(1.5f, 4f),
        new ContactAttackBehaviorDefinition(5, 0.8f, 1.2f)
    });

LevelDefinition level = new LevelDefinition(
    new LevelId("level-1"),
    goldReward: 10,
    enemies: new[] { new LevelEnemyDefinition(slime, 3) });
```

## Design Notes

- Behavior dispatch is type-based (`is` / pattern matching), not string discriminator based.
- Validation is enforced directly in constructors with guard clauses.
- The module is pure C# and does not depend on `UnityEngine`.
