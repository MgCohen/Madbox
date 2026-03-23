# Animation (App)

## TL;DR

- Purpose: Unity-facing animator helpers (`AnimationController`, `AnimationAttribute`) and clip event routing (`AnimationEventRouter`, optional `AnimationEventDefinition`).
- Location: `Assets/Scripts/App/Animation/Runtime/` (`Madbox.Animation` assembly), tests in `Assets/Scripts/App/Animation/Tests/` (`Madbox.Animation.Tests`).
- Depends on: `Madbox.Entities`, Unity engine.
- Used by: `Madbox.GameView` (player behaviors, combat presentation, attribute → animator drivers).

## Public API

| Symbol | Role |
|--------|------|
| `AnimationController` | Thin `Animator` wrapper: cross-fade play, bool/float parameters (string or `AnimationAttribute`). |
| `AnimationAttribute` | ScriptableObject id for an animator parameter name (`ParameterName` == asset name). |
| `AnimationEventDefinition` | ScriptableObject; `EventId` is the asset `name`; clips reference this asset on the animation event Object field. |
| `AnimationEventContext` | Read-only struct: `Animator` on the router object and matching `AnimationEventDefinition`. |
| `AnimationEventRouter` | Single Unity animation callback → multicast `Action<AnimationEventContext>` registered per `AnimationEventDefinition`. |
| `EntityAttributeAnimatorDriver<TData>` | Maps `EntityAttribute` → `AnimationAttribute` on `AnimationController` when attribute values change (generic over `EntityData` subclasses). |

## Testing

- Assembly: `Madbox.Animation.Tests` (EditMode).

## Related

- `Docs/App/GameView.md`
- `Docs/Core/Entities.md`
