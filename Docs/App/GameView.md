# App GameView

## TL;DR

- Purpose: Unity view for the white-box battle loop UI.
- Location: `Assets/Scripts/App/GameView/`.
- Depends on: `Scaffold.MVVM.View`, `Madbox.Battle`.

## Responsibilities

- Owns `GameView` as the UI adapter.
- Binds `GameState` text and Complete button visibility.
- Forwards frame ticks and Complete clicks to `Madbox.Battle.GameViewModel`.
- Does not own gameplay orchestration services.

## Public API

- `GameView` (`UIView<Madbox.Battle.GameViewModel>`).

## Testing

- Covered by bootstrap PlayMode flow test:
  - `Madbox.Bootstrap.PlayModeTests` -> `BootstrapWhiteBoxLoopPlayModeTests`.

## Notes

- This module intentionally remains a white-box UI shell.
- Game orchestration logic for this loop lives in `Core/Battle` and `Core/Levels`.
