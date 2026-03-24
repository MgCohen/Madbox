# Enemies (Meta)

## TL;DR

- Purpose: Unity-side enemy actors for the battle slice — `Enemy`, `EnemyService`, AI behaviors, and pooling.
- Location: `Assets/Scripts/Meta/Enemies/Runtime/` (`Madbox.Enemies`), tests `Madbox.Enemies.Tests`.
- Depends on: `Madbox.Entities`, `Madbox.Animation` (optional presentation), Unity engine.
- Used by: `Madbox.Battle` (`EnemyFactory`, `EnemyService`, session tick).
- Runtime/Editor: runtime gameplay.

Keywords: enemy, AI, behavior runner, pool, battle

## Responsibilities

- Owns: `Enemy`, `EnemyService`, `EnemyFactory`, `PrefabPool<T>`, behavior runners and bee unit behaviors, contact damage helpers.
- Does not own: level authoring (`Madbox.Levels`), battle rule evaluation, or UI HUD beyond optional world health bar component.
- Boundaries: Behaviors use the same first-accept-wins pattern as player; `EnemyFrameContextProvider` is session-owned, not prefab-embedded.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `Enemy` | `Entity` on enemy root: stats, `Initialize()` after spawn. | Spawn/pool | Active enemy | Uninitialized behavior until `Initialize()`. |
| `EnemyService` / `EnemyFactory` | Spawn/track enemies, tick for delayed destroys. | Level entries, pools | Alive `Enemy` set | Pool/prefab errors surface at spawn. |
| `PrefabPool<T>` | Warm/get/release/unload for a prefab type. | Prefab, callbacks | Pooled instances | Caller owns lifecycle. |
| `EnemyFrameContextProvider` | `IEntityFrameInputProvider<EnemyInputContext>`; `SetPlayerRoot` per session. | Player transform | Per-frame context | Must be wired by `BattleGameFactory`. |
| `IEnemyBehavior` / `EnemyBehaviorRunner` | Priority stack behaviors matching player pattern. | Frame tick | Behavior side effects | Runner skips until `Enemy.Initialize()`. |

## Setup / Integration

1. On enemy prefab root: `Enemy`, optional `Damageable`, `AnimationController`, `EntityAttributeAnimatorDriver`, ordered `IEnemyBehavior` components, `EnemyBehaviorRunner` (leave input provider empty).
2. Do **not** embed `EnemyFrameContextProvider` on prefabs — battle session creates and wires it.
3. Call `EnemyService.Tick(deltaTime)` every frame while battle runs.

## How to Use

1. Register enemies in `LevelDefinition` via Addressables references.
2. Let `BattleGameFactory` create `EnemyFrameContextProvider` and assign all runners.
3. Tune bee behaviors (`BeeDashAttackEnemyBehavior`, `BeeChaseEnemyBehavior`) on prefab.
4. Use `PrefabPool<Enemy>` when pooling manually (warm, get, release, unload).

## Examples

### Minimal

```csharp
enemyService.Tick(Time.deltaTime);
```

### Realistic

```csharp
var pool = new PrefabPool<Enemy>(enemyPrefab, onGet: e => e.Initialize());
pool.WarmUp(8);
Enemy enemy = pool.Get();
// ... battle ...
pool.Release(enemy);
pool.Unload();
```

### Guard / Error path

```csharp
// Forgot EnemyService.Tick: delayed destroys after death may never complete
```

## Best Practices

- Keep movement on XZ by default; opt into `Rigidbody` paths via component toggles.
- Start attack cooldown when the dash segment finishes (bee preset convention).
- Use `DamageKnockbackReceiver` / `DamageReactor` from Entities/Animation for consistent combat presentation.

## Anti-Patterns

- Embedding `EnemyFrameContextProvider` on each prefab — one provider per session.
- Skipping `EnemyService.Tick` — breaks delayed destroy timing.

## Testing

- Test assembly: `Madbox.Enemies.Tests` (EditMode).
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Enemies.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: `Enemy.Initialize()` before behavior tick; shared `EnemyFrameContextProvider` for all runners in a session.
- Allowed Dependencies: `Madbox.Entities`, `Madbox.Animation`, Unity.
- Forbidden Dependencies: App UI, MainMenu, direct LiveOps from enemy behaviors.
- Change Checklist: run `Madbox.Enemies.Tests` and `Madbox.Battle.Tests` for integration paths.
- Known Tricky Areas: spawn bounds vs arena `BoxCollider`; knockback and `OnTriggerStay` multi-hit (coordinated with `Damageable.damageDelaySeconds` on player).

## Related

- `Docs/Core/Entities.md`
- `Docs/App/Battle.md`
- `Docs/App/Animation.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard.
