# App MainMenu

## TL;DR

- Purpose: minimal Main Menu slice to validate bootstrap startup, DI, navigation + Addressables view loading, and MVVM bind propagation for gold display and level buttons.
- Location: `Assets/Scripts/App/MainMenu/`.
- Depends on: `Scaffold.MVVM.View`, `Scaffold.MVVM.ViewModel`, `Madbox.Gold`, `Madbox.Level` (`ILevelMenuService` / `AvailableLevel` from `LevelService`), `Madbox.Levels` (asset type on `AvailableLevel`), `VContainer`, `Scaffold.Navigation` (ViewModel base).

## Responsibilities

- Owns menu-local viewmodel/view flow for displaying current gold backed by `GoldWallet`.
- Owns button-to-viewmodel command routing for `+1` gold.
- Owns one button per **`AvailableLevel`** from **`ILevelMenuService.GetAvailableLevels()`** (LiveOps state + `LevelDefinition`); click logs the asset name to the Console.
- Does not own economy rules beyond invoking `IGoldService`.
- Does not start battles; game flow beyond menu is out of scope for this slice.

**LiveOps note:** Progression and completion APIs live on **`Madbox.Level.LevelService`** (`CompleteLevelAsync`). **`ILevelMenuService`** is the read model for the menu; it is implemented by the same `LevelService` that consumes **`LevelGameData`** and Addressables definitions.

## Public API

- `MainMenuViewModel.AddOneGold()`: increments gold through injected `IGoldService`.
- `MainMenuViewModel.AvailableLevels`: read-only list from `ILevelMenuService` for UI.
- `MainMenuView`: binds `viewModel.Gold` to TextMeshPro text, Add Gold to `AddOneGold`, and builds level buttons from `AvailableLevels`.

## Testing

- EditMode test assembly: `Madbox.MainMenu.Tests`.
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests"
```

## Notes

- All menu text uses `TextMeshProUGUI`.
- Main menu is opened by `BootstrapScope.OnBootstrapCompleted(...)`.
