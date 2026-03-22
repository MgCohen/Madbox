# Enemies (Core)

## Purpose

Unity-side enemy actors for the battle slice (`EnemyActor`, `EnemyService`). Behaviors are evaluated each frame through a small priority runner so higher-priority actions win for that frame, matching the player-side “first winning behavior” pattern.

## Public API

| Type | Role |
|------|------|
| `EnemyActor` | Spawn marker and `Initialize()` gate for pooled/Addressables instances. |
| `EnemyService` / `EnemyFactory` | Spawn and track alive `EnemyActor` instances. |
| `IEnemyActorBehavior` | One step of prioritized enemy logic; return `true` to claim the current frame. |
| `EnemyBehaviorRunner` | Walks a behavior list in order and runs the first behavior that claims the frame. |
| `BeeEnemyBrain` | Bee preset: dash-attack in range, otherwise chase toward the player tag or assigned target. |

## Usage

1. Add `BeeEnemyBrain` to an enemy root (see `Assets/Prefabs/Enemies/BeeEnemy.prefab`).
2. Assign `playerTarget` or rely on `Player` tag (Hero prefab is tagged `Player`).
3. Tune `attackRange`, dash and chase speeds in the inspector. Optional `Rigidbody` on the same object enables impulse dash or velocity chase.

## Design notes

- Movement defaults to transform deltas on the XZ plane; physics paths are opt-in when a non-kinematic `Rigidbody` is present and the corresponding toggles are enabled.
- Attack cooldown starts when the dash segment finishes, not when it begins.
