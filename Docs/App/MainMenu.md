# App MainMenu

## TL;DR

- Purpose: minimal Main Menu white-box slice to validate bootstrap startup, DI, navigation + addressables view loading, and MVVM bind propagation.
- Location: `Assets/Scripts/App/MainMenu/`.
- Depends on: `Scaffold.MVVM.View`, `Scaffold.MVVM.ViewModel`, `Scaffold.MVVM.Model`, `Madbox.Gold`, `VContainer`.

## Responsibilities

- Owns menu-local model/viewmodel/view flow for displaying current gold.
- Owns button-to-viewmodel command routing for `+1` gold.
- Does not own economy rules beyond invoking `IGoldService`.

## Public API

- `MainMenuViewModel.AddOneGold()`: increments gold through injected `IGoldService`.
- `MainMenuView`: binds `viewModel.Gold` to TextMeshPro text and button click to `AddOneGold`.

## Testing

- EditMode test assembly: `Madbox.MainMenu.Tests`.
- Run:

```powershell
& ".\.agents\scripts\run-editmode-tests.ps1" -AssemblyNames "Madbox.MainMenu.Tests"
```

## Notes

- All menu text uses `TextMeshProUGUI`.
- Main menu is opened by `BootstrapScope.OnBootstrapCompleted(...)`.
