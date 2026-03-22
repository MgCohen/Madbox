# Core Tutorial (LiveOps)

## TL;DR

- Purpose: client holder for `TutorialGameData` from aggregated `GameData` after `LiveOpsService.InitializeAsync` (per-step `TutorialStepEntry` states, same idea as `LevelStateEntry` on levels).
- Location: `Assets/Scripts/Core/Tutorial/Runtime/` (`Madbox.Tutorial`), installer `Madbox.Tutorial.Container`.
- Depends on: `Madbox.LiveOps`, DTO plugin.

## Responsibilities

- `TutorialService` extends `GameClientModuleBase<TutorialGameData>`; `InitializeAsync` assigns `protected data` from `ILiveOpsService.GetModuleData<TutorialGameData>()`.

## Registration

`TutorialInstaller` registers `TutorialService` as `IGameClientModule`, `IAsyncLayerInitializable`, and self. Invoked from `BootstrapCoreInstaller` **after** `LiveOpsInstaller`.

## Tests

EditMode: `Assets/Scripts/Core/Tutorial/Tests` (`TutorialServiceTests`).
