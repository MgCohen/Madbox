# Animation (App)

## TL;DR

- Purpose: Unity-facing animator helpers (`AnimationController`, `AnimationAttribute`), clip event routing (`AnimationEventRouter`), and combat presentation hooks (`DamageReactor`, `EntityAttributeAnimatorDriver`).
- Location: `Assets/Scripts/App/Animation/Runtime/` (`Madbox.Animation`), tests `Madbox.Animation.Tests`.
- Depends on: `Madbox.Entities`, Unity engine.
- Used by: `Madbox.GameView`, hero/enemy prefabs, bootstrap spawn flows.
- Runtime/Editor: runtime components.

Keywords: animator, AnimationAttribute, events, DamageReactor

## Responsibilities

- Owns: Animator wrapper, parameter ids, event routing from clips, attribute-to-animator bridging, optional damage-driven state plays.
- Does not own: Gameplay rules, AI, or Addressables.
- Boundaries: Presentation layer; pairs with `Entity` / `Damageable` from `Madbox.Entities`.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `AnimationController` | Thin `Animator` wrapper; cross-fade, bool/float by string or `AnimationAttribute`. | Play requests | Animator state | Missing params log/warn per Unity. |
| `AnimationAttribute` | ScriptableObject id for animator parameter (`ParameterName`). | Asset | Param name | N/A |
| `AnimationEventRouter` | Routes Unity animation events to multicast callbacks. | Clip events | Delegates | Unregistered events ignored. |
| `EntityAttributeAnimatorDriver` | Maps `EntityAttribute` → `AnimationAttribute` on value change. | Entity + controller | Param updates | Missing refs leave stale values. |
| `DamageReactor` | Plays damaged/death states on `Damageable` events. | Damage stream | Anim states | No-op if animator missing. |

## Setup / Integration

1. Add `AnimationController` + `Animator` on actor; create `AnimationAttribute` assets under project convention (`Assets/Data/AnimationAttributes/`).
2. For stats driving motion, add `EntityAttributeAnimatorDriver` and bind attribute/animation pairs.
3. For hit reactions, add `DamageReactor` alongside `Damageable`.

## How to Use

1. Reference `AnimationAttribute` assets from drivers instead of hard-coded strings where possible.
2. Wire `AnimationEventDefinition` on clips and subscribe via `AnimationEventRouter`.
3. On enemies/players, align `EntityAttribute` list entries with driver bindings.

## Examples

### Minimal

```csharp
animationController.SetBool(isMovingAttribute, true);
```

### Realistic

```csharp
// EntityAttributeAnimatorDriver syncs HP or speed attributes to animator floats
driver.OnEntityLinked(playerEntity);
```

### Guard / Error path

```csharp
// Mismatched ParameterName vs Animator Controller — parameter silently ineffective in Unity
```

## Best Practices

- Keep parameter names stable; use `AnimationAttribute` as single source of truth.
- Prefer event router over string compare in animation callbacks when using shared clips.

## Anti-Patterns

- Duplicating `AnimationAttribute` per prefab when a shared asset suffices.
- Driving combat rules from animator state alone — keep authority in gameplay code.

## Testing

- Test assembly: `Madbox.Animation.Tests` (EditMode).
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Animation.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: `AnimationAttribute.ParameterName` matches Controller param; drivers listen to `Entity` value changes.
- Allowed Dependencies: `Madbox.Entities`, Unity.
- Forbidden Dependencies: LiveOps, Battle factories.
- Change Checklist: run `Madbox.Animation.Tests`; update prefab docs if event contract changes.
- Known Tricky Areas: animation events object field must reference `AnimationEventDefinition` asset.

## Related

- `Docs/App/GameView.md`
- `Docs/Core/Entities.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard.
