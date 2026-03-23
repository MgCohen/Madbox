# Player (Meta)

## TL;DR

- Purpose: Player **entity** MonoBehaviour and ScriptableObject attribute types (`Player`, `PlayerAttribute`) shared by bootstrap spawn, Game View behaviors, and prefabs.
- Location: `Assets/Scripts/Meta/Player/Runtime/` (`Madbox.Player` assembly), tests in `Assets/Scripts/Meta/Player/Tests/` (`Madbox.Player.Tests`).
- Depends on: `Madbox.Entities`.
- Used by: `Madbox.Bootstrap.Runtime` (`PlayerFactory`), `Madbox.GameView` (behaviors, `WeaponVisualController` wiring), hero prefabs.

## Responsibilities

- Owns `Player` (`Entity` subclass with `IsAlive` / `CanMove` when attributes are assigned).
- Owns `PlayerAttribute` assets for player stat ids.
- Does **not** own movement/attack **view** logic (see `Madbox.GameView`).

## Public API

| Symbol | Role |
|--------|------|
| `Player` | Serialized attribute entries; `IsAlive` / `CanMove` via assigned `PlayerAttribute` references. |
| `PlayerAttribute` | `EntityAttribute` subclass; create via **Create > Madbox > Player > Player Attribute**. |

## Related

- `Docs/Core/Entities.md`
- `Docs/App/GameView.md`
