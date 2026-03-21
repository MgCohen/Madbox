# App GameView

## TL;DR

- Purpose: Unity view for the white-box battle loop UI.
- Location: `Assets/Scripts/App/GameView/`.
- Depends on: `Scaffold.MVVM.View`, `Madbox.Battle`.

## Responsibilities

- Owns `GameView` as the UI adapter.
- Binds `GameState` text and Complete button visibility.
- Forwards frame ticks and Complete clicks to `Madbox.Battle.GameViewModel`.
- Uses serialized scene/prefab configuration for `MainScene` gameplay controls (`Joystick` + `Hero`) instead of runtime auto-attach bootstrap.
- Owns virtual joystick input capture and normalized direction output.
- Owns player view-side movement behavior (transform movement + walk/idle animation switching).
- Owns player view-side attack behavior that triggers attack animation only for this milestone.
- Does not own gameplay orchestration services.

## Public API

- `GameView` (`UIView<Madbox.Battle.GameViewModel>`).
- `VirtualJoystickInput` (`MonoBehaviour`, pointer-driven 2D direction source).
- `PlayerMovementViewBehavior` (`MonoBehaviour`, joystick-driven movement + movement animation).
- `PlayerAttackAnimationBehavior` (`MonoBehaviour`, animation-only attack trigger).

## Testing

- Covered by bootstrap PlayMode flow test:
  - `Madbox.Bootstrap.PlayModeTests` -> `BootstrapWhiteBoxLoopPlayModeTests`.

## Notes

- This module intentionally remains a white-box UI shell.
- Game orchestration logic for this loop lives in `Core/Battle` and `Core/Levels`.
