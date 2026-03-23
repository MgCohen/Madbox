# Animation (App)

## TL;DR

- Purpose: Unity-facing animator helpers (`AnimationController`, `AnimationAttribute`) and clip event routing (`CharacterAnimationEventRouter`, `AnimationEventDefinition`).
- Location: `Assets/Scripts/App/Animation/Runtime/` (`Madbox.Animation` assembly), tests in `Assets/Scripts/App/Animation/Tests/` (`Madbox.Animation.Tests`).
- Depends on: Unity engine only.
- Used by: `Madbox.Entity` (attribute → animator driver), `Madbox.GameView` (player behaviors, combat presentation).

## Public API

| Symbol | Role |
|--------|------|
| `AnimationController` | Thin `Animator` wrapper: cross-fade play, bool/float parameters (string or `AnimationAttribute`). |
| `AnimationAttribute` | ScriptableObject id for an animator parameter name (`ParameterName` == asset name). |
| `AnimationEventDefinition` | ScriptableObject; `EventId` is the asset `name` (rename the file to change the clip string). |
| `CharacterAnimationEventRouter` | Single Unity animation callback → multicast handlers registered by `AnimationEventDefinition`. |

## Testing

- Assembly: `Madbox.Animation.Tests` (EditMode).

## Related

- `Docs/App/GameView.md`
- `Docs/App/Entity.md`
