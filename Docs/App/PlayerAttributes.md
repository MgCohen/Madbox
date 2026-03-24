# Player attributes (App / Meta)

## TL;DR

- Purpose: Document how **`Player`** and **`PlayerAttribute`** use **`Entity`** / **`EntityAttribute`** for stats, booleans, and animator binding — companion to `Docs/Meta/Player.md`.
- Location: Types live in `Assets/Scripts/Meta/Player/Runtime/` (`Madbox.Player`); this doc is conceptual glue for authoring.
- Depends on: `Madbox.Entities`, `Madbox.Animation` (`EntityAttributeAnimatorDriver`, `AnimationAttribute`).
- Used by: Hero prefabs, `Madbox.GameView` behaviors, bootstrap spawn/loadout.
- Runtime/Editor: runtime types; authoring in Editor.

Keywords: PlayerAttribute, EntityAttribute, animation mapping, bool convention

## Responsibilities

- Owns: Explains attribute/list conventions on `Player`, bool-as-float rules, and animator binding paths.
- Does not own: `Player` API surface (see `Docs/Meta/Player.md`) or `Entity` core behavior (`Docs/Core/Entities.md`).
- Boundaries: Documentation-only module note; no separate assembly.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `PlayerAttribute` | `EntityAttribute` subclass; asset name as id. | Asset authoring | Stat id | N/A at runtime if unassigned. |
| `Player` | Extends `Entity`; `IsAlive` / `CanMove` via dedicated `PlayerAttribute` refs. | Serialized entries | Property getters | Defaults if attributes missing. |
| `EntityAttributeAnimatorDriver` | Maps `EntityAttribute` → `AnimationAttribute`. | Entity + controller | Animator params | Missing bindings leave parameters stale. |

## Setup / Integration

1. Create gameplay `EntityAttribute` / `PlayerAttribute` assets under `Assets/Data/Attributes/`.
2. On `Player`, assign `IsAlive` and `CanMove` attribute assets and matching `attributeEntries` with default floats (`1` for true).
3. On behaviors, assign the same `PlayerAttribute` references as in the list for consistent reads.
4. Optional: add `EntityAttributeAnimatorDriver` bindings and `AnimationAttribute` assets under `Assets/Data/AnimationAttributes/`.

## How to Use

1. Follow bool-as-float convention: `> 0` true, `0` false; `SetBoolAttribute` writes `1` or `0`.
2. Keep animator parameter names aligned with `AnimationAttribute.ParameterName` (asset name).
3. When adding a stat, add one list entry on `Player` and reference the same asset from behaviors.

## Examples

### Minimal

```csharp
player.SetBoolAttribute(isAliveAttribute, true);
```

### Realistic

```
1. Create Player Attribute assets: IsAlive, CanMove, MaxHp.
2. Add entries on Player with base values; assign same refs on PlayerMovementViewBehavior for gating.
3. Bind MaxHp to Damageable max HP via Entities patterns (see Entities doc).
```

### Guard / Error path

```csharp
// Behavior references a different PlayerAttribute instance than the list — values appear out of sync; use one asset reference everywhere
```

## Best Practices

- Centralize attribute assets in shared folders; avoid per-unit one-off names unless needed.
- Mirror `EntityAttribute` list entries with behavior references to avoid drift.

## Anti-Patterns

- Duplicating string identifiers instead of shared `EntityAttribute` references.
- Mixing bool semantics without the `> 0` float convention.

## Testing

- Covered by `Madbox.Player.Tests` and `Madbox.Entities.Tests` as applicable.
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Player.Tests","Madbox.Entities.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: bool stats use float storage; `PlayerAttribute` ids come from asset references.
- Allowed Dependencies: Cross-doc only — implementation in `Madbox.Player` / `Madbox.Entities` / `Madbox.Animation`.
- Forbidden Dependencies: N/A (doc module).
- Change Checklist: update `Docs/Meta/Player.md` and `Docs/Core/Entities.md` when behavior changes.
- Known Tricky Areas: animator float vs bool modes on `EntityAttributeAnimatorDriver`.

## Related

- `Docs/Meta/Player.md`
- `Docs/Core/Entities.md`
- `Docs/App/Animation.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard.
