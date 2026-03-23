# Entities (Core)

## TL;DR

- Purpose: Reusable entity view data (float attributes with additive modifiers), behavior runner (first-accept-wins stack), and contracts for per-frame input and behaviors.
- Location: `Assets/Scripts/Core/Entity/Runtime/` (`Madbox.Entities` assembly), tests in `Assets/Scripts/Core/Entity/Tests/` (`Madbox.Entities.Tests`). *(Folder name `Entity` may be renamed to `Entities` in the editor; assembly is `Madbox.Entities`.)*
- Depends on: Unity engine only (no App-layer assemblies).
- Used by: `Madbox.Animation` (`EntityAttributeAnimatorDriver` bridges attributes to `AnimationController`), `Madbox.Player` (`PlayerData` / `PlayerAttribute`), `Madbox.Enemies` (`EnemyData` / `EnemyAttribute`), `Madbox.GameView` (`ProjectileData` and thin subclasses of generic runners/drivers).

## Public API

| Symbol | Role |
|--------|------|
| `EntityAttribute` | Base ScriptableObject id for a stat/flag (asset name as logical id via `AttributeName`). |
| `EntityAttributeEntry` | Serialized list item: attribute reference, base float, effective = base + modifiers, optional `UnityEvent<float>`. |
| `EntityAttributeModifierEntry` | Serializable pair: attribute reference + additive delta. |
| `EntityData` | `attributeEntries`, `attributeModifiers`, `Add/Remove/ClearAttributeModifier`, `Get/SetFloat/BoolAttribute` (set updates base), `AttributeValueChanged` (effective value). Recomputes on modifier changes, base changes, and after deserialize. Subclass for typed accessors. |
| `IEntityBehavior<TData,TInput>` | `TryAcceptControl`, `Execute`, `OnQuit` for ordered behaviors. |
| `IEntityFrameInputProvider<TInput>` | `GetFrameInput()` for runners that need per-frame context. |
| `EntityBehaviorRunner<TData,TInput>` | Runs behaviors in order; tracks active flow and `OnQuit` on switch. |

## Integration

- `EntityAttributeAnimatorDriver<TData>` lives in `Madbox.Animation` (`Assets/Scripts/App/Animation/Runtime/`): maps `EntityAttribute` → `AnimationAttribute` on `AnimationController`, and depends on `Madbox.Entities`.

## Testing

- Assembly: `Madbox.Entities.Tests` (EditMode) for base `EntityData` attribute behavior.

## Related

- `Docs/App/Animation.md`
- `Docs/App/GameView.md`
- `Docs/Meta/Player.md`
- `Docs/Meta/Enemies.md`
- `Docs/App/PlayerAttributes.md`
