# Enemies (Core)

## Purpose

Unity-side enemy actors for the battle slice (`EnemyActor`, `EnemyService`). Behaviors are evaluated each frame through a small priority runner so higher-priority actions win for that frame, matching the player-side “first winning behavior” pattern.

## Public API

| Type | Role |
|------|------|
| `Enemy` | Marker on the hit target for presentation (e.g. projectile `OnTriggerEnter`); place on the GameObject that owns the collider you expect hits against. |
| `EnemyActor` | Spawn marker and `Initialize()` gate for pooled/Addressables instances. |
| `EnemyService` / `EnemyFactory` | Spawn and track alive `EnemyActor` instances. |
| `PrefabPool<T>` | Lightweight per-prefab pool with `WarmUp`, `Get`, `Release`, and `Unload`. |
| `IEnemyActorBehavior` | One step of prioritized enemy logic; return `true` to claim the current frame. |
| `EnemyBehaviorRunner` | Walks a behavior list in order and runs the first behavior that claims the frame. |
| `BeeEnemyBrain` | Bee preset: dash-attack in range, otherwise chase toward the player tag or assigned target. |

## Usage

1. Add `BeeEnemyBrain` to an enemy root (see `Assets/Prefabs/Enemies/BeeEnemy.prefab`).
2. Assign `playerTarget` or rely on `Player` tag (Hero prefab is tagged `Player`).
3. Tune `attackRange`, dash and chase speeds in the inspector. Optional `Rigidbody` on the same object enables impulse dash or velocity chase.

Object pool example:

```csharp
var pool = new PrefabPool<EnemyActor>(
    enemyPrefab,
    onGet: enemy => enemy.Initialize());

pool.WarmUp(8);
EnemyActor enemy = pool.Get();
// ... gameplay usage ...
pool.Release(enemy);
pool.Unload();
```

## Design notes

- Movement defaults to transform deltas on the XZ plane; physics paths are opt-in when a non-kinematic `Rigidbody` is present and the corresponding toggles are enabled.
- Attack cooldown starts when the dash segment finishes, not when it begins.
- `PrefabPool<T>` is intentionally local/simple: no global pooling service and no Addressables coupling. Callers own prefab/handle serialization and choose if pools are shared or private.
