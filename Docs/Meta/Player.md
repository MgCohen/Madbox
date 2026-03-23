# Player (Meta)

## TL;DR

- Purpose: Player module contracts/data and shared player wiring types (`Player`, `PlayerAttribute`, `PlayerInputContext`, `IPlayerBehavior`, `PlayerBehaviorRunner`, `WeaponVisualController`, `PlayerWeaponController`) used by bootstrap spawn, Game View behaviors, and prefabs.
- Location: `Assets/Scripts/Meta/Player/Runtime/` (`Madbox.Player` assembly), tests in `Assets/Scripts/Meta/Player/Tests/` (`Madbox.Player.Tests`).
- Depends on: `Madbox.Entities`.
- Used by: `Madbox.Bootstrap.Runtime` (`PlayerFactory`), `Madbox.GameView` (behaviors), hero prefabs.

## Responsibilities

- Owns `Player` (`Entity` subclass with `IsAlive` / `CanMove` when attributes are assigned).
- Owns `PlayerAttribute` assets for player stat ids.
- Owns shared player runtime contracts used by view logic (`PlayerInputContext`, `IPlayerBehavior`) and the shared runner (`PlayerBehaviorRunner`).
- Owns player weapon visual synchronization helpers (`WeaponVisualController`, `PlayerWeaponController`) used by bootstrap loadout spawn flow.
- Does **not** own movement/attack **view** logic or animation-specific drivers (see `Madbox.GameView`).

## Public API

| Symbol | Role |
|--------|------|
| `Player` | Serialized attribute entries; `IsAlive` / `CanMove` via assigned `PlayerAttribute` references. |
| `PlayerAttribute` | `EntityAttribute` subclass; create via **Create > Madbox > Player > Player Attribute**. |
| `PlayerInputContext` | Immutable per-frame player input snapshot (`MoveDirection`). |
| `IPlayerBehavior` | Player behavior contract (`IEntityBehavior<Player, PlayerInputContext>`). |
| `PlayerBehaviorRunner` | Ordered behavior runner for `IPlayerBehavior` components. |
| `WeaponVisualController` | Maintains weapon sockets, active slot, and selection change event. |
| `PlayerWeaponController` | Subscribes to `Player.EquippedWeaponChanged` and updates `WeaponVisualController`. |

## Related

- `Docs/Core/Entities.md`
- `Docs/App/GameView.md`
