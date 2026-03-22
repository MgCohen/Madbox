# Entity (App)

## TL;DR

- Purpose: Reusable entity view data (float attributes), behavior runner (first-accept-wins stack), and animator sync from attribute changes.
- Location: `Assets/Scripts/App/Entity/Runtime/` (`Madbox.Entity` assembly), tests in `Assets/Scripts/App/Entity/Tests/` (`Madbox.Entity.Tests`).
- Depends on: `Madbox.Animation` (for `EntityAttributeAnimatorDriver`).
- Used by: `Madbox.GameView` (`PlayerData`, `EnemyData`, `ProjectileData`, and thin subclasses of generic runners/drivers).

## Public API

| Symbol | Role |
|--------|------|
| `EntityAttribute` | Base ScriptableObject id for a stat/flag (asset name as logical id via `AttributeName`). |
| `EntityAttributeEntry` | Serialized list item: attribute reference, float value, optional `UnityEvent<float>`. |
| `EntityData` | `attributeEntries`, `Get/SetFloat/BoolAttribute`, `AttributeValueChanged`. Subclass for typed accessors. |
| `IEntityBehavior<TData,TInput>` | `TryAcceptControl`, `Execute`, `OnQuit` for ordered behaviors. |
| `IEntityFrameInputProvider<TInput>` | `GetFrameInput()` for runners that need per-frame context. |
| `EntityBehaviorRunner<TData,TInput>` | Runs behaviors in order; tracks active flow and `OnQuit` on switch. |
| `EntityAttributeAnimatorDriver<TData>` | Maps `EntityAttribute` → `AnimationAttribute` on `AnimationController`. |

## Testing

- Assembly: `Madbox.Entity.Tests` (EditMode) for base `EntityData` attribute behavior.

## Related

- `Docs/App/Animation.md`
- `Docs/App/GameView.md`
- `Docs/App/PlayerAttributes.md`
