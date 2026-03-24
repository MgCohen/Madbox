# Player (Meta)

## TL;DR

- Purpose: Player module contracts, prefab behaviors (`Player`, `PlayerAttribute`, `PlayerBehaviorRunner`, movement/attack behaviors, weapon visuals) used by bootstrap spawn and hero prefabs.
- Location: `Assets/Scripts/Meta/Player/Runtime/` (`Madbox.Player`), tests `Madbox.Player.Tests`.
- Depends on: `Madbox.Entities`.
- Used by: `Madbox.Bootstrap.Runtime` (`PlayerFactory`), `Madbox.GameView`, hero prefabs.
- Runtime/Editor: runtime components.

Keywords: player, behaviors, weapon, IPlayerData

## Responsibilities

- Owns: `Player` (`Entity` subclass), `PlayerAttribute`, `PlayerInputContext`, `IPlayerBehavior`, `PlayerBehaviorRunner`, movement/attack view behaviors, `WeaponVisualController` / `PlayerWeaponController`, `PlayerWorldHealthHudBinder`.
- Does not own: LiveOps persistence, menu UI, or enemy AI.
- Boundaries: Prefab-level presentation + input; knockback via `DamageKnockbackReceiver` (`Madbox.Entities`); combat reactions via `DamageReactor` (`Madbox.Animation`).

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `Player` | Entity subclass; `IsAlive` / `CanMove` via attributes; implements `IPlayerData`. | Serialized attrs | State | Defaults if unassigned. |
| `PlayerAttribute` | `EntityAttribute` subclass for player stat ids. | Asset | Id | N/A |
| `PlayerInputContext` | Immutable move direction snapshot. | Frame input | Struct | N/A |
| `IPlayerBehavior` | `IEntityBehavior<Player, PlayerInputContext>`. | Runner | Behavior | Priority decides winner. |
| `PlayerBehaviorRunner` | Orders player behaviors. | Tick | Active behavior | Gates on attributes. |
| `PlayerMovementViewBehavior` / `PlayerAttackViewBehavior` | Locomotion and attack presentation. | Input + targets | Motion/damage | Requires wiring from `GameView`. |
| `WeaponVisualController` / `PlayerWeaponController` | Socket + active weapon visuals. | Loadout events | Visual swap | Missing refs skip bind. |

## Setup / Integration

1. On hero prefab: `Player`, `Damageable`, `PlayerBehaviorRunner`, movement/attack behaviors, optional `DamageKnockbackReceiver`, `DamageReactor` (Animation).
2. Assign `PlayerAttribute` assets consistently with `Docs/App/PlayerAttributes.md`.
3. `PlayerFactory` / bootstrap spawns player using loadout from Addressables (`Player Loadout`).

## How to Use

1. Configure attribute list and `isAlive` / `canMove` references on `Player`.
2. Order behaviors on the prefab; ensure runner receives `PlayerInputContext` from session code.
3. Hook weapons via `PlayerWeaponController` listening to equipped weapon changes.

## Examples

### Minimal

```csharp
var ctx = new PlayerInputContext(moveDirection);
runner.Tick(ctx);
```

### Realistic

```
Prefab: assign PlayerMovementViewBehavior + PlayerAttackViewBehavior, leave runner input wired from GameView session bridge.
```

### Guard / Error path

```csharp
// damageDelaySeconds on Damageable: mitigates trigger spam — tune for combat feel
```

## Best Practices

- Keep `IPlayerData` transform stable for enemy AI (`EnemyInputContext`).
- Align `PlayerAttribute` assets between list entries and behaviors (see PlayerAttributes doc).

## Anti-Patterns

- Embedding LiveOps calls in `Player` — keep progression in services.
- Duplicating weapon logic outside `PlayerWeaponController` / attack behavior.

## Testing

- Test assembly: `Madbox.Player.Tests` (EditMode).
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Player.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: `Player` is the `IPlayerData` for enemies; behaviors use same priority model as enemies.
- Allowed Dependencies: `Madbox.Entities`, `Madbox.Animation` for presentation hooks.
- Forbidden Dependencies: Direct MainMenu, LiveOps client in Player behaviours.
- Change Checklist: run `Madbox.Player.Tests` and `Madbox.GameView.Tests` when flow changes.
- Known Tricky Areas: knockback + movement authority; animation hit windows vs damage application.

## Related

- `Docs/Core/Entities.md`
- `Docs/App/PlayerAttributes.md`
- `Docs/App/GameView.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard.
