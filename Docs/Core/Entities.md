# Entities (Core)

## TL;DR

- Purpose: Reusable entity view data (float attributes with additive modifiers), behavior runner (first-accept-wins stack), and contracts for per-frame input and behaviors.
- Location: `Assets/Scripts/Core/Entity/Runtime/` (`Madbox.Entities`), tests `Assets/Scripts/Core/Entity/Tests/` (`Madbox.Entities.Tests`). *(Folder may appear as `Entity` in the editor; assembly is `Madbox.Entities`.)*
- **Unity coupling:** Under `Core/` for reuse but **not** pure domain — references `UnityEngine` (`ScriptableObject`, `MonoBehaviour`, etc.); `noEngineReferences: false`.
- Depends on: Unity engine only (no App-layer assemblies).
- Used by: `Madbox.Animation`, `Madbox.Player`, `Madbox.Enemies`, `Madbox.GameView`.

Keywords: entity, attributes, Damageable, behavior runner

## Responsibilities

- Owns: `Entity`/`EntityAttribute*` types, `Damageable`, knockback helper, behavior runner contracts, `IPlayerData`.
- Does not own: UI, navigation, LiveOps, or game mode orchestration.
- Boundaries: Engine-facing gameplay building blocks; MonoBehaviours allowed here per `Architecture.md`.

## Public API

| Symbol | Purpose | Inputs | Outputs | Failure behavior |
|---|---|---|---|---|
| `Entity` / `EntityAttribute*` | Attribute storage, modifiers, change notifications. | Serialized entries | Effective values | Missing refs yield default reads. |
| `Damageable` | HP pipeline with events, optional delay between hits, destroy delay hint. | Damage calls | Events, death | Invulnerability/delay may swallow hits. |
| `EntityBehaviorRunner<TData,TInput>` | Ordered behaviors; first accepting wins. | Frame tick | Active behavior | Skips when `ShouldRunTick()` false. |
| `IEntityBehavior<TData,TInput>` | `TryAcceptControl`, `Execute`, `OnQuit`. | Runner | Control flow | Lower priority may never run same frame. |
| `DamageKnockbackReceiver` | Planar knockback from damage events. | `TryApplyDamage` context | Transform/RB push | No-op if kinematic path not taken. |

## Setup / Integration

- Add `Entity` subclasses on actors; pair `Damageable` with max-HP `EntityAttribute` entry.
- `EntityAttributeAnimatorDriver` lives in `Madbox.Animation` — reference `Docs/App/Animation.md` for wiring.
- For enemies/players, see `Docs/Meta/Enemies.md` and `Docs/Meta/Player.md`.

## How to Use

1. Subclass `Entity` for typed accessors; add `EntityAttributeEntry` rows for each stat.
2. Use `Add/Remove/ClearAttributeModifier` for buffs/debuffs; listen to `AttributeValueChanged`.
3. Attach `Damageable` to health actors; wire `TryApplyDamage` from combat code.
4. Order `IEntityBehavior` components and drive with `EntityBehaviorRunner` + optional `IEntityFrameInputProvider`.

## Examples

### Minimal

```csharp
entity.SetFloatAttribute(hpAttribute, 100f);
float current = entity.GetFloatAttribute(hpAttribute);
```

### Realistic

```csharp
// Damageable with max HP attribute on same Entity
damageable.TryApplyDamage(10f, attackerPosition);
```

### Guard / Error path

```csharp
// damageDelaySeconds: rapid hits ignored — intentional i-frame style; set to 0 to disable
```

## Best Practices

- Keep `EntityAttribute` assets stable ids (asset reference as source of truth).
- Use additive modifiers for temporary effects; clear on teardown.
- Document destroy delays with orchestrators (`EnemyService`) when using `destroyDelayAfterDeathSeconds`.

## Anti-Patterns

- Duplicating stat storage outside `Entity` for the same logical attribute.
- Skipping runner `OnQuit` when switching behaviors — runner handles this when configured.

## Testing

- Test assembly: `Madbox.Entities.Tests` (EditMode).
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.Entities.Tests"
```

- Expected: all tests pass, zero failures.
- Bugfix rule: add/update regression test first.

## AI Agent Context

- Invariants: effective value = base + sum(modifiers); `Damageable` pairs with max-HP attribute entry.
- Allowed Dependencies: Unity only from this assembly’s perspective.
- Forbidden Dependencies: App, Meta, Infra assemblies.
- Change Checklist: run `Madbox.Entities.Tests`; update Animation/Player/Enemies docs if public surface changes.
- Known Tricky Areas: deserialize order vs modifier recompute; knockback vs kinematic RB.

## Related

- `Docs/App/Animation.md`
- `Docs/App/GameView.md`
- `Docs/Meta/Player.md`
- `Docs/Meta/Enemies.md`
- `Docs/App/PlayerAttributes.md`
- `Architecture.md`

## Changelog

- 2025-03-23: Restructured to module documentation standard (full tables and sections).
